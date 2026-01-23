using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using PdfProcessor.Core.Interfaces;
using System.Text.Json;

namespace PdfProcessor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EditPdfController : ControllerBase
{
  private readonly IPdfEditService _pdfEditService;
  private readonly ILogger<EditPdfController> _logger;

  public EditPdfController(
      IPdfEditService pdfEditService,
      ILogger<EditPdfController> logger)
  {
    _pdfEditService = pdfEditService;
    _logger = logger;
  }

  /// <summary>
  /// Obtém o número de páginas de um PDF
  /// </summary>
  [HttpPost("pagecount")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status400BadRequest)]
  public async Task<IActionResult> GetPageCount(IFormFile file)
  {
    try
    {
      if (file == null || file.Length == 0)
      {
        return BadRequest("Nenhum arquivo enviado");
      }

      _logger.LogInformation("📊 Contando páginas: {FileName}", file.FileName);

      using var stream = file.OpenReadStream();
      var pageCount = await _pdfEditService.GetPageCountAsync(stream);

      return Ok(new { pageCount, fileName = file.FileName });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "❌ Erro ao contar páginas");
      return StatusCode(500, $"Erro ao processar PDF: {ex.Message}");
    }
  }

  /// <summary>
  /// Gera thumbnail de uma página específica
  /// </summary>
  [HttpPost("thumbnail")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status400BadRequest)]
  public async Task<IActionResult> GetPageThumbnail(
      IFormFile file,
      [FromForm] int pageNumber)
  {
    try
    {
      if (file == null || file.Length == 0)
      {
        return BadRequest("Nenhum arquivo enviado");
      }

      if (pageNumber < 1)
      {
        return BadRequest("Número de página inválido");
      }

      _logger.LogDebug("🖼️ Gerando thumbnail: {FileName}, página {PageNumber}",
          file.FileName, pageNumber);

      using var stream = file.OpenReadStream();
      var thumbnail = await _pdfEditService.GeneratePageThumbnailAsync(stream, pageNumber);

      return Ok(new
      {
        pageNumber,
        thumbnail,
        fileName = file.FileName
      });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "❌ Erro ao gerar thumbnail da página {PageNumber}", pageNumber);
      return StatusCode(500, $"Erro ao gerar thumbnail: {ex.Message}");
    }
  }

  /// <summary>
  /// Processa PDF aplicando rotações e removendo páginas
  /// </summary>
  [HttpPost("process")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status400BadRequest)]
  public async Task<IActionResult> ProcessPdf(
      IFormFile file,
      [FromForm] string pageOperations)
  {
    try
    {
      if (file == null || file.Length == 0)
      {
        return BadRequest("Nenhum arquivo enviado");
      }

      if (string.IsNullOrEmpty(pageOperations))
      {
        return BadRequest("Operações de página não especificadas");
      }

      _logger.LogInformation("📝 Processando PDF: {FileName}", file.FileName);

      // Deserializar operações
      var operations = JsonSerializer.Deserialize<List<PageOperation>>(
          pageOperations,
          new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

      if (operations == null || operations.Count == 0)
      {
        return BadRequest("Nenhuma operação especificada");
      }

      _logger.LogInformation("🔧 Operações recebidas: {Count} páginas",
          operations.Count);

      using var stream = file.OpenReadStream();
      var result = await _pdfEditService.ProcessPdfAsync(
          stream,
          operations,
          file.FileName);

      if (!result.Success)
      {
        return StatusCode(500, $"Erro ao processar PDF: {result.ErrorMessage}");
      }

      // Gerar nome do arquivo de saída
      var outputFileName = Path.GetFileNameWithoutExtension(file.FileName) + "_edited.pdf";

      _logger.LogInformation(
          "✅ PDF processado: {Original} → {Final} páginas",
          result.OriginalPageCount, result.FinalPageCount);

      return File(
          result.ProcessedPdfData!,
          "application/pdf",
          outputFileName);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "❌ Erro ao processar PDF");
      return StatusCode(500, $"Erro ao processar PDF: {ex.Message}");
    }
  }

  /// <summary>
  /// Health check
  /// </summary>
  [HttpGet("health")]
  public IActionResult Health()
  {
    return Ok(new
    {
      service = "EditPdf",
      status = "healthy",
      timestamp = DateTime.UtcNow
    });
  }
}