using Microsoft.AspNetCore.Mvc;
using PdfProcessor.Core.Interfaces;
using PdfSharp.Pdf.IO;

namespace PdfProcessor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MergePdfController : ControllerBase
{
    private readonly IPdfMergeService _mergeService;
    private readonly ILogger<MergePdfController> _logger;

    public MergePdfController(
        IPdfMergeService mergeService,
        ILogger<MergePdfController> logger)
    {
        _mergeService = mergeService;
        _logger = logger;
    }

    /// <summary>
    /// Mescla múltiplos PDFs em um único arquivo
    /// </summary>
    [HttpPost("batch")]
    public async Task<IActionResult> MergeBatch(
        [FromForm] List<IFormFile> files,
        [FromForm] List<string>? pageRanges = null)
    {
        try
        {
            if (files == null || files.Count < 2)
            {
                return BadRequest("Envie pelo menos 2 arquivos PDF");
            }

            _logger.LogInformation($"🔗 Mesclando {files.Count} arquivo(s)");

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

                if (file.Length > 16 * 1024 * 1024) // 16MB
                {
                    return BadRequest($"Arquivo {file.FileName} excede 16MB");
                }
            }

            // Preparar streams e ranges
            var fileStreams = new Dictionary<string, Stream>();
            var rangeDict = new Dictionary<string, string>();

            try
            {
                for (int i = 0; i < files.Count; i++)
                {
                    var file = files[i];
                    var stream = new MemoryStream();
                    await file.CopyToAsync(stream);
                    stream.Position = 0;

                    fileStreams[file.FileName] = stream;

                    // Adicionar range se fornecido
                    if (pageRanges != null && i < pageRanges.Count && !string.IsNullOrWhiteSpace(pageRanges[i]))
                    {
                        rangeDict[file.FileName] = pageRanges[i];
                    }
                }

                // Mesclar PDFs
                var mergedPdf = await _mergeService.MergePdfsAsync(fileStreams, rangeDict);

                _logger.LogInformation($"✅ Mesclagem concluída: {mergedPdf.Length} bytes");

                // Retornar PDF mesclado
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var fileName = $"pdf_mesclado_{timestamp}.pdf";

                return File(mergedPdf, "application/pdf", fileName);
            }
            finally
            {
                // Limpar streams
                foreach (var stream in fileStreams.Values)
                {
                    stream.Dispose();
                }
            }
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning($"⚠️ Erro de validação: {ex.Message}");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao mesclar PDFs");
            return StatusCode(500, $"Erro ao mesclar PDFs: {ex.Message}");
        }
    }

    /// <summary>
    /// Conta o número de páginas de um PDF
    /// </summary>
    [HttpPost("pagecount")]
    public async Task<IActionResult> GetPageCount([FromForm] IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("Arquivo não fornecido");
            }

            if (!file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Arquivo não é um PDF");
            }

            _logger.LogInformation($"📄 Contando páginas de: {file.FileName}");

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            // Abrir PDF e contar páginas
            using var document = PdfReader.Open(stream, PdfDocumentOpenMode.InformationOnly);
            var pageCount = document.PageCount;

            _logger.LogInformation($"✅ {file.FileName}: {pageCount} página(s)");

            return Ok(new { fileName = file.FileName, pageCount });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ Erro ao contar páginas de {file?.FileName}");
            return StatusCode(500, $"Erro ao contar páginas: {ex.Message}");
        }
    }

    /// <summary>
    /// Extrai páginas específicas de um PDF
    /// </summary>
    [HttpPost("extract")]
    public async Task<IActionResult> ExtractPages(
        [FromForm] IFormFile file,
        [FromForm] string pageRange)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("Arquivo não fornecido");
            }

            if (!file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Arquivo não é um PDF");
            }

            if (string.IsNullOrWhiteSpace(pageRange))
            {
                return BadRequest("Range de páginas não fornecido");
            }

            _logger.LogInformation($"✂️ Extraindo páginas de: {file.FileName} (range: {pageRange})");

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            // Contar páginas primeiro
            int totalPages;
            using (var document = PdfReader.Open(stream, PdfDocumentOpenMode.InformationOnly))
            {
                totalPages = document.PageCount;
            }

            // Parsear range
            var pageNumbers = _mergeService.ParsePageRange(pageRange, totalPages);

            // Extrair páginas
            stream.Position = 0;
            var extractedPdf = await _mergeService.ExtractPagesAsync(stream, pageNumbers);

            _logger.LogInformation($"✅ {pageNumbers.Count} página(s) extraída(s)");

            var fileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_paginas_{pageRange.Replace(",", "_").Replace("-", "a")}.pdf";

            return File(extractedPdf, "application/pdf", fileName);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning($"⚠️ Erro de validação: {ex.Message}");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao extrair páginas");
            return StatusCode(500, $"Erro ao extrair páginas: {ex.Message}");
        }
    }
}