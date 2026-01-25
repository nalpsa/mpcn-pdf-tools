using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using Microsoft.Extensions.Logging;

namespace PdfProcessor.Infrastructure.Parsers;

public class PdfDebugHelper
{
    private readonly ILogger _logger;

    public PdfDebugHelper(ILogger logger)
    {
        _logger = logger;
    }

    public void DebugPdfStructure(Stream pdfStream)
    {
        pdfStream.Position = 0;
        using var reader = new PdfReader(pdfStream);

        _logger.LogInformation($"📊 === DEBUG PDF STRUCTURE ===");
        _logger.LogInformation($"Total de páginas: {reader.NumberOfPages}");
        _logger.LogInformation($"PDF Version: {reader.PdfVersion}");
        _logger.LogInformation($"Is Encrypted: {reader.IsEncrypted()}");
        _logger.LogInformation($"File Length: {reader.FileLength}");

        // Tentar extrair com TODAS as estratégias
        for (int pageNum = 1; pageNum <= Math.Min(3, reader.NumberOfPages); pageNum++)
        {
            _logger.LogInformation($"\n📄 === PÁGINA {pageNum} ===");

            // Estratégia 1: SimpleTextExtractionStrategy
            try
            {
                var simpleText = PdfTextExtractor.GetTextFromPage(reader, pageNum, new SimpleTextExtractionStrategy());
                _logger.LogInformation($"SimpleTextExtractionStrategy ({simpleText.Length} chars):");
                _logger.LogInformation(simpleText.Substring(0, Math.Min(500, simpleText.Length)));
            }
            catch (Exception ex)
            {
                _logger.LogError($"SimpleTextExtractionStrategy falhou: {ex.Message}");
            }

            // Estratégia 2: LocationTextExtractionStrategy
            try
            {
                var locationText = PdfTextExtractor.GetTextFromPage(reader, pageNum, new LocationTextExtractionStrategy());
                _logger.LogInformation($"\nLocationTextExtractionStrategy ({locationText.Length} chars):");
                _logger.LogInformation(locationText.Substring(0, Math.Min(500, locationText.Length)));
            }
            catch (Exception ex)
            {
                _logger.LogError($"LocationTextExtractionStrategy falhou: {ex.Message}");
            }

            // Estratégia 3: Chunks via custom strategy
            try
            {
                var customStrategy = new DebugTextExtractionStrategy();
                PdfTextExtractor.GetTextFromPage(reader, pageNum, customStrategy);
                var chunks = customStrategy.GetChunks();
                
                _logger.LogInformation($"\nChunks encontrados: {chunks.Count}");
                
                foreach (var chunk in chunks.Take(20))
                {
                    _logger.LogInformation($"  [{chunk.X:F1}, {chunk.Y:F1}] = \"{chunk.Text}\"");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Custom strategy falhou: {ex.Message}");
            }

            // Estratégia 4: Raw content stream
            try
            {
                var page = reader.GetPageN(pageNum);
                var contentBytes = reader.GetPageContent(pageNum);
                var content = System.Text.Encoding.UTF8.GetString(contentBytes);
                
                _logger.LogInformation($"\nRaw Content Stream ({content.Length} chars):");
                _logger.LogInformation(content.Substring(0, Math.Min(500, content.Length)));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Raw content stream falhou: {ex.Message}");
            }

            // Info sobre resources
            try
            {
                var page = reader.GetPageN(pageNum);
                var resources = page.GetAsDict(PdfName.RESOURCES);
                
                if (resources != null)
                {
                    _logger.LogInformation($"\nPage Resources:");
                    
                    var fonts = resources.GetAsDict(PdfName.FONT);
                    if (fonts != null)
                    {
                        _logger.LogInformation($"  Fonts: {fonts.Size}");
                    }
                    
                    var xObjects = resources.GetAsDict(PdfName.XOBJECT);
                    if (xObjects != null)
                    {
                        _logger.LogInformation($"  XObjects: {xObjects.Size}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Resources check falhou: {ex.Message}");
            }
        }
    }

    private class DebugTextExtractionStrategy : ITextExtractionStrategy
    {
        private List<TextChunk> chunks = new List<TextChunk>();

        public void BeginTextBlock() { }
        public void EndTextBlock() { }

        public void RenderText(TextRenderInfo renderInfo)
        {
            var text = renderInfo.GetText();
            var baseline = renderInfo.GetBaseline();
            var startPoint = baseline.GetStartPoint();

            chunks.Add(new TextChunk
            {
                Text = text,
                X = startPoint[iTextSharp.text.pdf.parser.Vector.I1],
                Y = startPoint[iTextSharp.text.pdf.parser.Vector.I2]
            });
        }

        public string GetResultantText()
        {
            return string.Join(" ", chunks.Select(c => c.Text));
        }

        public void RenderImage(ImageRenderInfo renderInfo) { }

        public List<TextChunk> GetChunks() => chunks;
    }

    public class TextChunk
    {
        public string Text { get; set; } = "";
        public float X { get; set; }
        public float Y { get; set; }
    }
}
