using Microsoft.AspNetCore.Mvc;
using PdfProcessor.Core.Interfaces;

namespace PdfProcessor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JuliusBarController : ControllerBase
{
    private readonly IJuliusBarParser _parser;
    private readonly ILogger<JuliusBarController> _logger;

    public JuliusBarController(IJuliusBarParser parser, ILogger<JuliusBarController> logger)
    {
        _parser = parser;
        _logger = logger;
    }

    [HttpPost("process")]
    public async Task<IActionResult> ProcessJuliusBar([FromForm] List<IFormFile> files)
    {
        try
        {
            if (files == null || files.Count == 0)
            {
                return BadRequest("Nenhum arquivo enviado");
            }

            _logger.LogInformation($"🏦 Recebidos {files.Count} arquivo(s) Julius Bär");

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
                $"JuliusBar_Transactions_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao processar Julius Bär");
            return StatusCode(500, $"Erro: {ex.Message}");
        }
    }
}
