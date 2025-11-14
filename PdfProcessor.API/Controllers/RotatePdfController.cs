using Microsoft.AspNetCore.Mvc;
using PdfProcessor.Core.Interfaces;
using PdfProcessor.Core.Enums;
using System.IO.Compression;

namespace PdfProcessor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RotatePdfController : ControllerBase
{
  private readonly IPdfRotateService _rotateService;
  private readonly ILogger<RotatePdfController> _logger;

  public RotatePdfController(
      IPdfRotateService rotateService,
      ILogger<RotatePdfController> logger)
  {
    _rotateService = rotateService;
    _logger = logger;
  }

  /// <summary>
  /// Gera miniatura de um PDF
  /// </summary>
  [HttpPost("thumbnail")]
  [RequestSizeLimit(16 * 1024 * 1024)] // 16MB
  public async Task<IActionResult> GenerateThumbnail(IFormFile file)
  {
    try
    {
      if (file == null || file.Length == 0)
        return BadRequest("Nenhum arquivo enviado");

      if (!file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        return BadRequest("Apenas arquivos PDF são permitidos");

      _logger.LogInformation("Gerando thumbnail para: {FileName}", file.FileName);

      using var stream = file.OpenReadStream();
      var thumbnailBytes = await _rotateService.GenerateThumbnailAsync(stream);

      // Converter bytes para base64
      var thumbnailBase64 = Convert.ToBase64String(thumbnailBytes);

      // ✅ CORREÇÃO: Usar image/png ao invés de image/svg+xml
      var dataUrl = $"data:image/png;base64,{thumbnailBase64}";

      _logger.LogInformation("Thumbnail gerado com sucesso: {Size} bytes", thumbnailBytes.Length);

      return Ok(new { thumbnail = dataUrl, fileName = file.FileName });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Erro ao gerar miniatura");
      return StatusCode(500, "Erro ao processar arquivo");
    }
  }

  /// <summary>
  /// Obtém informações de um PDF
  /// </summary>
  [HttpPost("info")]
  [RequestSizeLimit(16 * 1024 * 1024)]
  public async Task<IActionResult> GetPdfInfo(IFormFile file)
  {
    try
    {
      if (file == null || file.Length == 0)
        return BadRequest("Nenhum arquivo enviado");

      if (!file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        return BadRequest("Apenas arquivos PDF são permitidos");

      using var stream = file.OpenReadStream();
      var info = await _rotateService.GetPdfInfoAsync(stream);
      info.FileName = file.FileName;

      return Ok(info);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Erro ao obter informações do PDF");
      return StatusCode(500, "Erro ao processar arquivo");
    }
  }

  /// <summary>
  /// Rotaciona múltiplos PDFs
  /// POST /api/RotatePdf/batch
  /// </summary>
  [HttpPost("batch")]
  [RequestSizeLimit(100 * 1024 * 1024)] // 100MB total
  public async Task<IActionResult> RotateBatch([FromForm] RotatePdfBatchRequest request)
  {
    try
    {
      if (request.Files == null || request.Files.Count == 0)
        return BadRequest("Nenhum arquivo enviado");

      _logger.LogInformation("Processando {Count} arquivo(s) para rotação", request.Files.Count);

      // Preparar streams e rotações
      var pdfStreams = new Dictionary<string, Stream>();
      var rotations = new Dictionary<string, RotationAngle>();

      for (int i = 0; i < request.Files.Count; i++)
      {
        var file = request.Files[i];
        var fileName = file.FileName;

        // Converter graus para RotationAngle enum
        var degrees = i < request.Rotations.Count ? request.Rotations[i] : 0;
        var rotationAngle = ConvertDegreesToRotationAngle(degrees);

        if (rotationAngle != RotationAngle.None)
        {
          pdfStreams[fileName] = file.OpenReadStream();
          rotations[fileName] = rotationAngle;
        }
      }

      if (pdfStreams.Count == 0)
      {
        return BadRequest("Nenhum arquivo com rotação definida");
      }

      // Rotacionar PDFs
      var rotatedFiles = await _rotateService.RotateBatchAsync(pdfStreams, rotations);

      // Se apenas 1 arquivo, retornar diretamente
      if (rotatedFiles.Count == 1)
      {
        var single = rotatedFiles.First();
        var outputName = Path.GetFileNameWithoutExtension(single.Key) + "_rotacionado.pdf";

        return File(single.Value, "application/pdf", outputName);
      }

      // Múltiplos arquivos: criar ZIP
      var zipBytes = CreateZip(rotatedFiles);
      var zipName = $"pdfs_rotacionados_{DateTime.Now:yyyyMMdd_HHmmss}.zip";

      _logger.LogInformation("ZIP criado com sucesso: {FileName}", zipName);

      return File(zipBytes, "application/zip", zipName);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Erro ao rotacionar PDFs");
      return StatusCode(500, $"Erro ao processar: {ex.Message}");
    }
  }

  /// <summary>
  /// Cria arquivo ZIP com múltiplos PDFs
  /// </summary>
  private static byte[] CreateZip(Dictionary<string, byte[]> files)
  {
    using var zipStream = new MemoryStream();

    // Usar CompressionLevel do System.IO.Compression explicitamente
    using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
    {
      foreach (var file in files)
      {
        var entry = archive.CreateEntry(file.Key, System.IO.Compression.CompressionLevel.Optimal);
        using var entryStream = entry.Open();
        entryStream.Write(file.Value, 0, file.Value.Length);
      }
    }

    return zipStream.ToArray();
  }

  /// <summary>
  /// Converte graus para RotationAngle enum
  /// </summary>
  private static RotationAngle ConvertDegreesToRotationAngle(int degrees)
  {
    // Normalizar
    degrees = degrees % 360;
    if (degrees < 0) degrees += 360;

    return degrees switch
    {
      0 => RotationAngle.None,
      90 => RotationAngle.Rotate90,
      180 => RotationAngle.Rotate180,
      270 => RotationAngle.Rotate270,
      _ => RotationAngle.None
    };
  }
}

/// <summary>
/// Request para rotação em lote
/// </summary>
public class RotatePdfBatchRequest
{
  public List<IFormFile> Files { get; set; } = new();
  public List<int> Rotations { get; set; } = new();
  public List<int> Indices { get; set; } = new();
}