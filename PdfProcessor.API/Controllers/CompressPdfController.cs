using Microsoft.AspNetCore.Mvc;
using PdfProcessor.Core.Interfaces;
using System.IO.Compression;
using CoreCompressionLevel = PdfProcessor.Core.Enums.CompressionLevel;

namespace PdfProcessor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CompressPdfController : ControllerBase
{
  private readonly IPdfCompressService _compressService;
  private readonly ILogger<CompressPdfController> _logger;

  public CompressPdfController(
      IPdfCompressService compressService,
      ILogger<CompressPdfController> logger)
  {
    _compressService = compressService;
    _logger = logger;
  }

  /// <summary>
  /// Comprime múltiplos PDFs em lote
  /// POST /api/CompressPdf/batch
  /// </summary>
  [HttpPost("batch")]
  [RequestSizeLimit(100 * 1024 * 1024)] // 100MB total
  public async Task<IActionResult> CompressBatch([FromForm] CompressPdfBatchRequest request)
  {
    try
    {
      if (request.Files == null || request.Files.Count == 0)
        return BadRequest("Nenhum arquivo enviado");

      _logger.LogInformation("Processando {Count} arquivo(s) para compressão", request.Files.Count);

      // Preparar streams e configurações
      var pdfStreams = new Dictionary<string, Stream>();
      var compressionSettings = new Dictionary<string, CompressionSettings>();

      for (int i = 0; i < request.Files.Count; i++)
      {
        var file = request.Files[i];
        var fileName = file.FileName;

        // Obter configurações para este arquivo
        var level = i < request.CompressionLevels.Count
            ? ParseCompressionLevel(request.CompressionLevels[i])
            : CoreCompressionLevel.Medium;

        var removeImages = i < request.RemoveImages.Count && request.RemoveImages[i];

        pdfStreams[fileName] = file.OpenReadStream();
        compressionSettings[fileName] = new CompressionSettings
        {
          Level = level,
          RemoveImages = removeImages
        };
      }

      // Comprimir PDFs
      var results = await _compressService.CompressBatchAsync(pdfStreams, compressionSettings);

      // Filtrar apenas sucessos
      var successfulResults = results
          .Where(r => r.Value.Success && r.Value.CompressedData != null)
          .ToDictionary(r => r.Key, r => r.Value.CompressedData!);

      if (successfulResults.Count == 0)
      {
        return BadRequest("Nenhum arquivo foi comprimido com sucesso");
      }

      // ✅ CORREÇÃO: Se apenas 1 arquivo, retornar como PDF direto
      if (successfulResults.Count == 1)
      {
        var single = successfulResults.First();
        var originalResult = results[single.Key];
        var outputName = Path.GetFileNameWithoutExtension(single.Key) + "_comprimido.pdf";

        _logger.LogInformation("Retornando PDF único comprimido: {FileName} " +
            "({Original} → {Compressed}, {Percentage})",
            outputName,
            originalResult.OriginalSizeMB,
            originalResult.CompressedSizeMB,
            originalResult.CompressionPercentage);

        // ✅ IMPORTANTE: Retornar como application/pdf com extensão .pdf
        return File(single.Value, "application/pdf", outputName);
      }

      // Múltiplos arquivos: criar ZIP
      var zipBytes = CreateZip(successfulResults);
      var zipName = $"pdfs_comprimidos_{DateTime.Now:yyyyMMdd_HHmmss}.zip";

      // Log de resumo
      var totalOriginal = results.Values.Sum(r => r.OriginalSizeBytes);
      var totalCompressed = results.Values.Where(r => r.Success).Sum(r => r.CompressedSizeBytes);
      var overallPercentage = totalOriginal > 0
          ? $"{(1 - (double)totalCompressed / totalOriginal) * 100:F2}%"
          : "0%";

      _logger.LogInformation("ZIP criado com sucesso: {FileName} " +
          "({Original} MB → {Compressed} MB, economia de {Percentage})",
          zipName,
          totalOriginal / 1024.0 / 1024.0,
          totalCompressed / 1024.0 / 1024.0,
          overallPercentage);

      // ✅ IMPORTANTE: Retornar como application/zip com extensão .zip
      return File(zipBytes, "application/zip", zipName);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Erro ao comprimir PDFs");
      return StatusCode(500, $"Erro ao processar: {ex.Message}");
    }
  }

  /// <summary>
  /// Obtém informações de um PDF (tamanho, páginas, etc.)
  /// POST /api/CompressPdf/info
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

      // Comprimir temporariamente para estimar tamanho comprimido
      var result = await _compressService.CompressPdfAsync(
          stream,
          CoreCompressionLevel.Medium,
          false,
          file.FileName);

      return Ok(new
      {
        fileName = file.FileName,
        originalSize = file.Length,
        originalSizeMB = $"{file.Length / 1024.0 / 1024.0:F2} MB",
        estimatedCompressedSize = result.CompressedSizeBytes,
        estimatedCompressedSizeMB = result.CompressedSizeMB,
        estimatedSavings = result.CompressionPercentage,
        canCompress = result.Success
      });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Erro ao obter informações do PDF");
      return StatusCode(500, "Erro ao processar arquivo");
    }
  }

  /// <summary>
  /// Cria arquivo ZIP com múltiplos PDFs
  /// </summary>
  private static byte[] CreateZip(Dictionary<string, byte[]> files)
  {
    using var zipStream = new MemoryStream();

    using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
    {
      foreach (var file in files)
      {
        // Adicionar sufixo "_comprimido" ao nome
        var baseName = Path.GetFileNameWithoutExtension(file.Key);
        var extension = Path.GetExtension(file.Key);
        var newName = $"{baseName}_comprimido{extension}";

        var entry = archive.CreateEntry(newName, CompressionLevel.Optimal);
        using var entryStream = entry.Open();
        entryStream.Write(file.Value, 0, file.Value.Length);
      }
    }

    return zipStream.ToArray();
  }

  /// <summary>
  /// Converte string para CompressionLevel enum
  /// </summary>
  private static CoreCompressionLevel ParseCompressionLevel(string level)
  {
    return level?.ToLower() switch
    {
      "low" or "baixo" => CoreCompressionLevel.Low,
      "medium" or "medio" or "médio" => CoreCompressionLevel.Medium,
      "high" or "alto" => CoreCompressionLevel.High,
      _ => CoreCompressionLevel.Medium
    };
  }
}

/// <summary>
/// Request para compressão em lote
/// </summary>
public class CompressPdfBatchRequest
{
  public List<IFormFile> Files { get; set; } = new();
  public List<string> CompressionLevels { get; set; } = new(); // "low", "medium", "high"
  public List<bool> RemoveImages { get; set; } = new();
}