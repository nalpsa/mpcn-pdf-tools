using PdfProcessor.Core.Interfaces;
using Microsoft.Extensions.Logging;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Docnet.Core;

namespace PdfProcessor.Infrastructure.PdfServices;

public class PdfEditService : IPdfEditService
{
  private readonly ILogger<PdfEditService> _logger;

  public PdfEditService(ILogger<PdfEditService> logger)
  {
    _logger = logger;
  }

  public async Task<string> GeneratePageThumbnailAsync(Stream pdfStream, int pageNumber, int maxWidth = 200)
  {
    return await Task.Run(() =>
    {
      try
      {
        pdfStream.Position = 0;

        // Copiar stream para array de bytes (Docnet precisa de byte[])
        byte[] pdfBytes;
        using (var ms = new MemoryStream())
        {
          pdfStream.CopyTo(ms);
          pdfBytes = ms.ToArray();
        }

        // Usar Docnet.Core para renderizar a página específica
        using var library = DocLib.Instance;
        using var docReader = library.GetDocReader(pdfBytes, new Docnet.Core.Models.PageDimensions(1024, 1024));

        if (docReader.GetPageCount() < pageNumber)
        {
          throw new ArgumentException($"Página {pageNumber} inválida. PDF tem {docReader.GetPageCount()} páginas.");
        }

        // Renderizar página (pageNumber - 1 porque Docnet usa índice 0-based)
        using var pageReader = docReader.GetPageReader(pageNumber - 1);
        var rawBytes = pageReader.GetImage();
        var pageWidth = pageReader.GetPageWidth();
        var pageHeight = pageReader.GetPageHeight();

        // Converter raw bytes para imagem usando ImageSharp
        using var image = Image.LoadPixelData<Bgra32>(rawBytes, pageWidth, pageHeight);

        // Calcular dimensões proporcionais
        var width = maxWidth;
        var height = (int)((double)pageHeight / pageWidth * maxWidth);

        // Redimensionar mantendo proporção
        image.Mutate(x => x.Resize(new ResizeOptions
        {
          Size = new Size(width, height),
          Mode = ResizeMode.Max
        }));

        // Converter para PNG em base64
        using var outputStream = new MemoryStream();
        image.SaveAsPng(outputStream);
        var base64 = Convert.ToBase64String(outputStream.ToArray());

        return $"data:image/png;base64,{base64}";
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Erro ao gerar thumbnail da página {PageNumber}", pageNumber);
        return GenerateErrorThumbnail(pageNumber);
      }
    });
  }

  private string GenerateErrorThumbnail(int pageNumber)
  {
    try
    {
      // Criar imagem de erro 200x250
      using var image = new Image<Rgba32>(200, 250);
      
      image.Mutate(x => x.BackgroundColor(Color.FromRgb(255, 245, 245)));

      using var outputStream = new MemoryStream();
      image.SaveAsPng(outputStream);
      var base64 = Convert.ToBase64String(outputStream.ToArray());

      return $"data:image/png;base64,{base64}";
    }
    catch
    {
      // Fallback para SVG se ImageSharp falhar
      var svg = $@"<svg xmlns='http://www.w3.org/2000/svg' width='200' height='250'>
  <rect width='100%' height='100%' fill='#fff5f5' stroke='#fc8181' stroke-width='2' rx='4'/>
  <text x='50%' y='50%' font-family='system-ui' font-size='16' fill='#e53e3e' text-anchor='middle'>Erro ao carregar</text>
  <text x='50%' y='65%' font-family='system-ui' font-size='12' fill='#fc8181' text-anchor='middle'>Página {pageNumber}</text>
</svg>";

      var svgBytes = Encoding.UTF8.GetBytes(svg);
      return $"data:image/svg+xml;base64,{Convert.ToBase64String(svgBytes)}";
    }
  }

  public async Task<int> GetPageCountAsync(Stream pdfStream)
  {
    return await Task.Run(() =>
    {
      try
      {
        pdfStream.Position = 0;
        using var pdfDoc = PdfReader.Open(pdfStream, PdfDocumentOpenMode.Import);
        var count = pdfDoc.PageCount;

        _logger.LogInformation("📊 PDF tem {PageCount} páginas", count);

        return count;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Erro ao contar páginas do PDF");
        throw;
      }
    });
  }

  public async Task<PdfEditResult> ProcessPdfAsync(
      Stream pdfStream,
      List<PageOperation> pageOperations,
      string originalFileName = "")
  {
    return await Task.Run(() =>
    {
      var result = new PdfEditResult();

      try
      {
        pdfStream.Position = 0;

        using var inputPdfDoc = PdfReader.Open(pdfStream, PdfDocumentOpenMode.Import);
        result.OriginalPageCount = inputPdfDoc.PageCount;

        _logger.LogInformation("📝 Editando PDF: {FileName} ({PageCount} páginas)",
                originalFileName, result.OriginalPageCount);

        int pagesRotated = 0;
        int pagesKept = 0;

        using var outputPdfDoc = new PdfDocument();

        foreach (var operation in pageOperations.OrderBy(o => o.PageNumber))
        {
          if (operation.PageNumber < 1 || operation.PageNumber > result.OriginalPageCount)
          {
            _logger.LogWarning("⚠️ Página {PageNumber} inválida, ignorando", operation.PageNumber);
            continue;
          }

          if (!operation.Keep)
          {
            _logger.LogDebug("🗑️ Removendo página {PageNumber}", operation.PageNumber);
            continue;
          }

          // COPIAR PÁGINA (PdfSharp usa índice 0-based)
          var sourcePage = inputPdfDoc.Pages[operation.PageNumber - 1];
          var copiedPage = outputPdfDoc.AddPage(sourcePage);

          // APLICAR ROTAÇÃO
          if (operation.Rotation != 0)
          {
            var currentRotation = (int)copiedPage.Rotate;
            var newRotation = (currentRotation + operation.Rotation) % 360;

            if (newRotation < 0) newRotation += 360;

            copiedPage.Rotate = newRotation;
            pagesRotated++;

            _logger.LogDebug("🔄 Página {PageNumber}: {Current}° → {New}°",
                    operation.PageNumber, currentRotation, newRotation);
          }

          pagesKept++;
        }

        if (outputPdfDoc.PageCount == 0)
        {
          throw new InvalidOperationException("Nenhuma página foi adicionada ao PDF editado");
        }

        // SALVAR PDF
        using var ms = new MemoryStream();
        outputPdfDoc.Save(ms, false);
        result.ProcessedPdfData = ms.ToArray();

        result.FinalPageCount = pagesKept;
        result.PagesRotated = pagesRotated;
        result.PagesRemoved = result.OriginalPageCount - pagesKept;
        result.Success = true;

        _logger.LogInformation("✅ PDF editado: {Original} → {Final} páginas ({Rotated} rot, {Removed} rem) - {Bytes} bytes",
                result.OriginalPageCount, result.FinalPageCount, result.PagesRotated, result.PagesRemoved, result.ProcessedPdfData.Length);

        return result;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "❌ Erro ao processar PDF: {FileName}", originalFileName);
        result.Success = false;
        result.ErrorMessage = ex.Message;
        return result;
      }
    });
  }
}