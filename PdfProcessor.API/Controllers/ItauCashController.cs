using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PdfProcessor.Core.Interfaces;

namespace PdfProcessor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ItauCashController : ControllerBase
{
    private readonly IItauCashParser _parser;
    private readonly ILogger<ItauCashController> _logger;

    public ItauCashController(
        IItauCashParser parser,
        ILogger<ItauCashController> logger)
    {
        _parser = parser;
        _logger = logger;
    }

    /// <summary>
    /// Processa múltiplos PDFs de Cash Transactions do Itaú
    /// </summary>
    [HttpPost("batch")]
    public async Task<IActionResult> ProcessBatch([FromForm] List<IFormFile> files)
    {
        try
        {
            if (files == null || files.Count == 0)
            {
                return BadRequest("Nenhum arquivo enviado");
            }

            _logger.LogInformation($"🏦 Processando {files.Count} extrato(s) Itaú Cash");

            // Validar arquivos
            foreach (var file in files)
            {
                if (!file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest($"Arquivo {file.FileName} não é um PDF");
                }

                if (file.Length == 0)
                {
                    return BadRequest($"Arquivo {file.FileName} está vazio");
                }

                if (file.Length > 16 * 1024 * 1024)
                {
                    return BadRequest($"Arquivo {file.FileName} excede 16MB");
                }
            }

            // Converter para streams
            var streams = new List<Stream>();
            
            try
            {
                foreach (var file in files)
                {
                    var stream = new MemoryStream();
                    await file.CopyToAsync(stream);
                    stream.Position = 0;
                    streams.Add(stream);
                }

                // Processar
                var excelBytes = await _parser.ProcessBatchAsync(streams);

                _logger.LogInformation($"✅ Processamento concluído: {excelBytes.Length} bytes");

                // Retornar Excel
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var fileName = $"itau_cash_{timestamp}.xlsx";

                return File(excelBytes, 
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                    fileName);
            }
            finally
            {
                // Limpar streams
                foreach (var stream in streams)
                {
                    stream?.Dispose();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao processar extratos");
            return StatusCode(500, $"Erro ao processar: {ex.Message}");
        }
    }
}