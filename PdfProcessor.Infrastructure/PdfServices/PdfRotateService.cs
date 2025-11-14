using PdfProcessor.Core.Interfaces;
using PdfProcessor.Core.Enums;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using Docnet.Core;
using Docnet.Core.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;

namespace PdfProcessor.Infrastructure.PdfServices;

/// <summary>
/// Serviço para rotação de PDFs usando PdfSharp e Docnet.Core para thumbnails
/// </summary>
public class PdfRotateService : IPdfRotateService
{
  /// <summary>
  /// Rotaciona um único PDF
  /// </summary>
  public async Task<byte[]> RotatePdfAsync(Stream pdfStream, RotationAngle rotationAngle, string fileName)
  {
    return await Task.Run(() =>
    {
      using var outputStream = new MemoryStream();

      // Abrir PDF
      pdfStream.Position = 0;
      using var document = PdfReader.Open(pdfStream, PdfDocumentOpenMode.Modify);

      // Converter RotationAngle para graus
      int degrees = ConvertRotationAngleToDegrees(rotationAngle);

      // Rotacionar todas as páginas
      foreach (PdfPage page in document.Pages)
      {
        page.Rotate = NormalizeRotation(page.Rotate + degrees);
      }

      // Salvar
      document.Save(outputStream, false);
      return outputStream.ToArray();
    });
  }

  /// <summary>
  /// Rotaciona múltiplos PDFs retornando dicionário com nome e bytes
  /// </summary>
  public async Task<Dictionary<string, byte[]>> RotateBatchAsync(
      Dictionary<string, Stream> files,
      Dictionary<string, RotationAngle> rotations)
  {
    var results = new Dictionary<string, byte[]>();

    foreach (var pdf in files)
    {
      var fileName = pdf.Key;
      var stream = pdf.Value;

      // Obter rotação para este arquivo (default None)
      var rotation = rotations.ContainsKey(fileName) ? rotations[fileName] : RotationAngle.None;

      if (rotation == RotationAngle.None)
      {
        // Sem rotação, apenas copiar
        stream.Position = 0;
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        results[fileName] = ms.ToArray();
      }
      else
      {
        // Rotacionar
        var rotatedBytes = await RotatePdfAsync(stream, rotation, fileName);
        results[fileName] = rotatedBytes;
      }
    }

    return results;
  }

  /// <summary>
  /// Obtém informações de um PDF
  /// </summary>
  public async Task<PdfInfo> GetPdfInfoAsync(Stream pdfStream)
  {
    return await Task.Run(() =>
    {
      pdfStream.Position = 0;
      using var document = PdfReader.Open(pdfStream, PdfDocumentOpenMode.Import);

      return new PdfInfo
      {
        PageCount = document.PageCount,
        Title = document.Info.Title ?? string.Empty,
        Author = document.Info.Author ?? string.Empty,
        Subject = document.Info.Subject ?? string.Empty,
        FileName = string.Empty // Será preenchido pelo controller
      };
    });
  }

  /// <summary>
  /// Gera miniatura (primeira página) de um PDF como PNG em bytes
  /// </summary>
  public async Task<byte[]> GenerateThumbnailAsync(Stream pdfStream, int width = 200, int height = 200)
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

        // Usar Docnet.Core para renderizar primeira página
        using var library = DocLib.Instance;
        using var docReader = library.GetDocReader(pdfBytes, new PageDimensions(width * 2, height * 2));

        if (docReader.GetPageCount() == 0)
        {
          return GeneratePlaceholderImage(width, height);
        }

        using var pageReader = docReader.GetPageReader(0); // Primeira página
        var rawBytes = pageReader.GetImage();
        var pageWidth = pageReader.GetPageWidth();
        var pageHeight = pageReader.GetPageHeight();

        // Converter raw bytes para imagem usando ImageSharp
        using var image = Image.LoadPixelData<Bgra32>(rawBytes, pageWidth, pageHeight);

        // Redimensionar mantendo proporção
        image.Mutate(x => x.Resize(new ResizeOptions
        {
          Size = new Size(width, height),
          Mode = ResizeMode.Max
        }));

        // Converter para PNG
        using var outputStream = new MemoryStream();
        image.SaveAsPng(outputStream);
        return outputStream.ToArray();
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Erro ao gerar thumbnail: {ex.Message}");
        return GeneratePlaceholderImage(width, height);
      }
    });
  }

  /// <summary>
  /// Gera imagem placeholder quando não consegue renderizar o PDF
  /// </summary>
  private static byte[] GeneratePlaceholderImage(int width, int height)
  {
    // Criar imagem simples com fundo cinza
    using var image = new Image<Rgba32>(width, height);

    // Preencher com cor de fundo
    var backgroundColor = Color.FromRgb(247, 250, 252);
    for (int y = 0; y < height; y++)
    {
      for (int x = 0; x < width; x++)
      {
        image[x, y] = new Rgba32(247, 250, 252, 255);
      }
    }

    // Salvar como PNG
    using var outputStream = new MemoryStream();
    var encoder = new PngEncoder();
    image.Save(outputStream, encoder);
    return outputStream.ToArray();
  }

  /// <summary>
  /// Converte RotationAngle enum para graus
  /// </summary>
  private static int ConvertRotationAngleToDegrees(RotationAngle angle)
  {
    return angle switch
    {
      RotationAngle.None => 0,
      RotationAngle.Rotate90 => 90,
      RotationAngle.Rotate180 => 180,
      RotationAngle.Rotate270 => 270,
      _ => 0
    };
  }

  /// <summary>
  /// Normaliza ângulo de rotação (0, 90, 180, 270)
  /// </summary>
  private static int NormalizeRotation(int rotation)
  {
    rotation = rotation % 360;
    if (rotation < 0) rotation += 360;
    return rotation;
  }
}