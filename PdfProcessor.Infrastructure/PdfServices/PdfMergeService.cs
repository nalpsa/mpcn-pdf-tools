using Microsoft.Extensions.Logging;
using PdfProcessor.Core.Interfaces;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace PdfProcessor.Infrastructure.PdfServices;

public class PdfMergeService : IPdfMergeService
{
    private readonly ILogger<PdfMergeService> _logger;

    public PdfMergeService(ILogger<PdfMergeService> logger)
    {
        _logger = logger;
    }

    public async Task<byte[]> MergePdfsAsync(
        Dictionary<string, Stream> files,
        Dictionary<string, string>? pageRanges = null)
    {
        _logger.LogInformation($"🔗 Iniciando mesclagem de {files.Count} arquivo(s)");

        return await Task.Run(() =>
        {
            using var outputDocument = new PdfDocument();

            int fileIndex = 0;
            foreach (var (fileName, stream) in files)
            {
                try
                {
                    _logger.LogInformation($"📄 Processando arquivo {fileIndex + 1}/{files.Count}: {fileName}");

                    stream.Position = 0;
                    using var inputDocument = PdfReader.Open(stream, PdfDocumentOpenMode.Import);

                    // Obter range de páginas
                    string? pageRange = pageRanges?.GetValueOrDefault(fileName);
                    List<int> pagesToInclude;

                    if (string.IsNullOrWhiteSpace(pageRange))
                    {
                        // Se não houver range, incluir todas as páginas
                        pagesToInclude = Enumerable.Range(1, inputDocument.PageCount).ToList();
                        _logger.LogInformation($"   Range: todas as páginas (1-{inputDocument.PageCount})");
                    }
                    else
                    {
                        // Parsear range especificado
                        pagesToInclude = ParsePageRange(pageRange, inputDocument.PageCount);
                        _logger.LogInformation($"   Range: {pageRange} → {pagesToInclude.Count} página(s)");
                    }

                    // Adicionar páginas ao documento de saída
                    foreach (var pageNumber in pagesToInclude)
                    {
                        // PdfSharp usa índice 0-based
                        var page = inputDocument.Pages[pageNumber - 1];
                        outputDocument.AddPage(page);
                    }

                    _logger.LogInformation($"✅ {pagesToInclude.Count} página(s) adicionada(s) de {fileName}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"❌ Erro ao processar {fileName}");
                    throw new InvalidOperationException($"Erro ao processar {fileName}: {ex.Message}", ex);
                }

                fileIndex++;
            }

            if (outputDocument.PageCount == 0)
            {
                throw new InvalidOperationException("Nenhuma página foi adicionada ao PDF mesclado");
            }

            _logger.LogInformation($"✅ Mesclagem concluída: {outputDocument.PageCount} página(s) no total");

            // Salvar em memória
            using var ms = new MemoryStream();
            outputDocument.Save(ms, false);
            return ms.ToArray();
        });
    }

    public List<int> ParsePageRange(string pageRange, int totalPages)
    {
        if (string.IsNullOrWhiteSpace(pageRange))
        {
            throw new ArgumentException("Range de páginas não pode estar vazio");
        }

        var pages = new HashSet<int>();
        var parts = pageRange.Split(',', StringSplitOptions.RemoveEmptyEntries);

        foreach (var part in parts)
        {
            var trimmedPart = part.Trim();

            if (trimmedPart.Contains('-'))
            {
                // Range como "3-7"
                var rangeParts = trimmedPart.Split('-');
                if (rangeParts.Length != 2)
                {
                    throw new ArgumentException($"Range inválido: '{trimmedPart}'");
                }

                if (!int.TryParse(rangeParts[0].Trim(), out int start) ||
                    !int.TryParse(rangeParts[1].Trim(), out int end))
                {
                    throw new ArgumentException($"Range inválido: '{trimmedPart}'");
                }

                if (start < 1 || end < 1)
                {
                    throw new ArgumentException($"Números de página devem ser maiores que 0");
                }

                if (start > end)
                {
                    throw new ArgumentException($"Range inválido: início ({start}) maior que fim ({end})");
                }

                if (start > totalPages || end > totalPages)
                {
                    throw new ArgumentException($"Página fora do limite: máximo é {totalPages}");
                }

                for (int i = start; i <= end; i++)
                {
                    pages.Add(i);
                }
            }
            else
            {
                // Página individual
                if (!int.TryParse(trimmedPart, out int pageNum))
                {
                    throw new ArgumentException($"Número de página inválido: '{trimmedPart}'");
                }

                if (pageNum < 1)
                {
                    throw new ArgumentException($"Número de página deve ser maior que 0");
                }

                if (pageNum > totalPages)
                {
                    throw new ArgumentException($"Página {pageNum} fora do limite: máximo é {totalPages}");
                }

                pages.Add(pageNum);
            }
        }

        if (pages.Count == 0)
        {
            throw new ArgumentException("Nenhuma página válida no range especificado");
        }

        return pages.OrderBy(p => p).ToList();
    }

    public async Task<byte[]> ExtractPagesAsync(Stream pdfStream, List<int> pageNumbers)
    {
        _logger.LogInformation($"✂️ Extraindo {pageNumbers.Count} página(s)");

        return await Task.Run(() =>
        {
            pdfStream.Position = 0;
            using var inputDocument = PdfReader.Open(pdfStream, PdfDocumentOpenMode.Import);
            using var outputDocument = new PdfDocument();

            // Validar números de páginas
            foreach (var pageNumber in pageNumbers)
            {
                if (pageNumber < 1 || pageNumber > inputDocument.PageCount)
                {
                    throw new ArgumentException($"Página {pageNumber} fora do limite (1-{inputDocument.PageCount})");
                }

                // PdfSharp usa índice 0-based
                var page = inputDocument.Pages[pageNumber - 1];
                outputDocument.AddPage(page);
            }

            _logger.LogInformation($"✅ {pageNumbers.Count} página(s) extraída(s)");

            using var ms = new MemoryStream();
            outputDocument.Save(ms, false);
            return ms.ToArray();
        });
    }
}