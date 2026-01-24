using Microsoft.AspNetCore.Mvc;
using PdfProcessor.Core.Interfaces;

namespace PdfProcessor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ItauCash2Controller : ControllerBase
{
    private readonly IItauCash2Parser _parser;
    private readonly ILogger<ItauCash2Controller> _logger;

    public ItauCash2Controller(IItauCash2Parser parser, ILogger<ItauCash2Controller> logger)
    {
        _parser = parser;
        _logger = logger;
    }

    [HttpPost("process")]
    public async Task<IActionResult> ProcessItauCash2([FromForm] List<IFormFile> files)
    {
        try
        {
            if (files == null || files.Count == 0)
            {
                return BadRequest("Nenhum arquivo enviado");
            }

            _logger.LogInformation($"🦁 Recebidos {files.Count} arquivo(s) Itaú Cash 2.0");

            var streams = new List<Stream>();
            foreach (var file in files)
            {
                var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                ms.Position = 0;
                streams.Add(ms);
                
                _logger.LogInformation($"  📄 {file.FileName} ({file.Length} bytes)");
            }

            var excelBytes = await _parser.ProcessBatchAsync(streams);

            // Cleanup
            foreach (var stream in streams)
            {
                stream.Dispose();
            }

            _logger.LogInformation($"✅ Processamento concluído: {excelBytes.Length} bytes");

            return File(
                excelBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"ItauCash2_Transactions_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao processar Itaú Cash 2.0");
            return StatusCode(500, $"Erro: {ex.Message}");
        }
    }
}