using PdfProcessor.Core.Interfaces;
using Microsoft.Extensions.Logging;
using iText.Kernel.Pdf;
using System.Text;

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

        using var pdfDoc = new PdfDocument(new PdfReader(pdfStream));

        if (pageNumber < 1 || pageNumber > pdfDoc.GetNumberOfPages())
        {
          throw new ArgumentException($"Página {pageNumber} inválida. PDF tem {pdfDoc.GetNumberOfPages()} páginas.");
        }

        var page = pdfDoc.GetPage(pageNumber);
        var pageSize = page.GetPageSize();

        var width = maxWidth;
        var height = (int)(pageSize.GetHeight() / pageSize.GetWidth() * maxWidth);

        var svg = GenerateSvgThumbnail(pageNumber, width, height, pageSize);

        var svgBytes = Encoding.UTF8.GetBytes(svg);
        var base64 = Convert.ToBase64String(svgBytes);

        return $"data:image/svg+xml;base64,{base64}";
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Erro ao gerar thumbnail da página {PageNumber}", pageNumber);
        return GenerateErrorThumbnail(pageNumber);
      }
    });
  }

  private string GenerateSvgThumbnail(int pageNumber, int width, int height, iText.Kernel.Geom.Rectangle pageSize)
  {
    var orientation = pageSize.GetWidth() > pageSize.GetHeight() ? "Paisagem" : "Retrato";
    var iconX = width / 2 - 30;
    var iconY = height / 2 - 70;

    return $@"<svg xmlns='http://www.w3.org/2000/svg' width='{width}' height='{height}' viewBox='0 0 {width} {height}'>
  <rect width='100%' height='100%' fill='white' stroke='#cbd5e0' stroke-width='2' rx='4'/>
  <rect width='100%' height='10' fill='#667eea' opacity='0.9' rx='4'/>
  <g transform='translate({iconX}, {iconY})'>
    <rect x='0' y='0' width='60' height='80' fill='#e2e8f0' stroke='#a0aec0' stroke-width='2' rx='2'/>
    <polygon points='60,0 60,15 45,0' fill='#cbd5e0' stroke='#a0aec0' stroke-width='1'/>
    <line x1='10' y1='25' x2='50' y2='25' stroke='#718096' stroke-width='2' stroke-linecap='round'/>
    <line x1='10' y1='35' x2='50' y2='35' stroke='#718096' stroke-width='2' stroke-linecap='round'/>
    <line x1='10' y1='45' x2='40' y2='45' stroke='#718096' stroke-width='2' stroke-linecap='round'/>
  </g>
  <text x='50%' y='{height - 60}' font-family='system-ui, -apple-system, sans-serif' font-size='24' font-weight='bold' fill='#2d3748' text-anchor='middle' dominant-baseline='middle'>Página {pageNumber}</text>
  <text x='50%' y='{height - 35}' font-family='system-ui, -apple-system, sans-serif' font-size='11' fill='#718096' text-anchor='middle' dominant-baseline='middle'>{pageSize.GetWidth():F0} × {pageSize.GetHeight():F0} pts</text>
  <text x='50%' y='{height - 20}' font-family='system-ui, -apple-system, sans-serif' font-size='10' fill='#a0aec0' text-anchor='middle' dominant-baseline='middle'>{orientation}</text>
</svg>";
  }

  private string GenerateErrorThumbnail(int pageNumber)
  {
    var svg = $@"<svg xmlns='http://www.w3.org/2000/svg' width='200' height='250'>
  <rect width='100%' height='100%' fill='#fff5f5' stroke='#fc8181' stroke-width='2' rx='4'/>
  <text x='50%' y='50%' font-family='system-ui' font-size='16' fill='#e53e3e' text-anchor='middle'>Erro ao carregar</text>
  <text x='50%' y='65%' font-family='system-ui' font-size='12' fill='#fc8181' text-anchor='middle'>Página {pageNumber}</text>
</svg>";

    var svgBytes = Encoding.UTF8.GetBytes(svg);
    return $"data:image/svg+xml;base64,{Convert.ToBase64String(svgBytes)}";
  }

  public async Task<int> GetPageCountAsync(Stream pdfStream)
  {
    return await Task.Run(() =>
    {
      try
      {
        pdfStream.Position = 0;
        using var pdfDoc = new PdfDocument(new PdfReader(pdfStream));
        var count = pdfDoc.GetNumberOfPages();

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

        using var inputPdfDoc = new PdfDocument(new PdfReader(pdfStream));
        result.OriginalPageCount = inputPdfDoc.GetNumberOfPages();

        _logger.LogInformation("📝 Editando PDF: {FileName} ({PageCount} páginas)",
                originalFileName, result.OriginalPageCount);

        int pagesRotated = 0;
        int pagesKept = 0;

        // Criar MemoryStream que NÃO será fechado pelo writer
        var outputStream = new MemoryStream();

        // Usar using aninhados COM SetCloseStream(false)
        using (var writer = new PdfWriter(outputStream))
        {
          // CRÍTICO: Não fechar o stream quando writer for disposed
          writer.SetCloseStream(false);

          using (var outputPdfDoc = new PdfDocument(writer))
          {
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

              var sourcePage = inputPdfDoc.GetPage(operation.PageNumber);
              var copiedPage = sourcePage.CopyTo(outputPdfDoc);

              if (operation.Rotation != 0)
              {
                var currentRotation = copiedPage.GetRotation();
                var newRotation = (currentRotation + operation.Rotation) % 360;

                if (newRotation < 0) newRotation += 360;

                copiedPage.SetRotation(newRotation);
                pagesRotated++;

                _logger.LogDebug("🔄 Página {PageNumber}: {Current}° → {New}°",
                        operation.PageNumber, currentRotation, newRotation);
              }

              pagesKept++;
            }
          } // outputPdfDoc é fechado aqui
        } // writer é fechado aqui (mas NÃO fecha o stream)

        // AGORA o stream ainda está aberto e podemos acessá-lo
        outputStream.Position = 0;
        result.ProcessedPdfData = outputStream.ToArray();

        // Agora sim podemos fechar o stream
        outputStream.Dispose();

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