using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using PdfProcessor.Core.Interfaces;
using PdfProcessor.Core.Models;

namespace PdfProcessor.Infrastructure.Parsers;

public class MorganStanleyParser : IMorganStanleyParser
{
  public async Task<List<MorganStanleyTransaction>> ParsePdfAsync(Stream pdfStream, string fileName)
  {
    var transactions = new List<MorganStanleyTransaction>();

    return await Task.Run(() =>
    {
      try
      {
        Console.WriteLine($"\n🔍 === MORGAN STANLEY - MODO DEBUG ===");
        Console.WriteLine($"📄 Arquivo: {fileName}\n");

        pdfStream.Position = 0;
        using var pdfReader = new PdfReader(pdfStream);

        int totalPages = pdfReader.NumberOfPages;
        Console.WriteLine($"📄 Total de páginas: {totalPages}\n");

        // Processar apenas primeira página para debug
        for (int pageNum = 1; pageNum <= Math.Min(2, totalPages); pageNum++)
        {
          Console.WriteLine($"📄 === PÁGINA {pageNum} ===\n");

          var strategy = new LocationTextExtractionStrategyEx();
          var text = PdfTextExtractor.GetTextFromPage(pdfReader, pageNum, strategy);

          var chunks = strategy.GetTextChunks();

          // 1. PROCURAR ACCOUNT NUMBER
          var lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

          Console.WriteLine("🔍 Procurando Account Number...");
          for (int i = 0; i < lines.Length; i++)
          {
            if (lines[i].Contains("Select UMA Active Assets Account") && i + 1 < lines.Length)
            {
              Console.WriteLine($"✅ Account encontrado: {lines[i + 1]}");
              break;
            }
          }
          Console.WriteLine();

          // 2. PROCURAR CABEÇALHO DA TABELA
          var headerChunks = chunks
                  .Where(c => c.Text.Contains("Activity") || c.Text.Contains("Date") ||
                             c.Text.Contains("Settlement") || c.Text.Contains("Type") ||
                             c.Text.Contains("Description") || c.Text.Contains("Comments") ||
                             c.Text.Contains("Quantity") || c.Text.Contains("Price") ||
                             c.Text.Contains("Credits") || c.Text.Contains("Debits"))
                  .OrderBy(c => c.Y)
                  .ThenBy(c => c.X)
                  .ToList();

          if (headerChunks.Any())
          {
            Console.WriteLine("📋 CABEÇALHOS DA TABELA:");
            foreach (var chunk in headerChunks.Take(20))
            {
              Console.WriteLine($"  X={chunk.X,6:F1}  Y={chunk.Y,6:F1}  Text=\"{chunk.Text}\"");
            }
            Console.WriteLine();
          }

          // 3. PROCURAR PRIMEIRA LINHA DE DADOS
          var lineGroups = chunks
                  .GroupBy(c => Math.Round(c.Y, 1))
                  .OrderByDescending(g => g.Key)
                  .ToList();

          bool foundHeader = false;
          int linesShown = 0;

          foreach (var lineGroup in lineGroups)
          {
            var lineChunks = lineGroup.OrderBy(c => c.X).ToList();
            var lineText = string.Join("", lineChunks.Select(c => c.Text));

            // Detectar cabeçalho
            if (lineText.Contains("CASH FLOW ACTIVITY BY DATE"))
            {
              foundHeader = true;
              Console.WriteLine("✅ Início da tabela detectado\n");
              continue;
            }

            // Mostrar primeiras 5 linhas após cabeçalho
            if (foundHeader && linesShown < 5)
            {
              Console.WriteLine($"📊 LINHA {linesShown + 1}:");
              Console.WriteLine($"   Texto: {lineText.Substring(0, Math.Min(100, lineText.Length))}...\n");
              Console.WriteLine("   POSIÇÕES X:");

              foreach (var chunk in lineChunks.Take(15))
              {
                Console.WriteLine($"     X={chunk.X,6:F1}  Text=\"{chunk.Text}\"");
              }
              Console.WriteLine();

              linesShown++;
            }

            if (linesShown >= 5) break;
          }
        }

        Console.WriteLine("✅ Análise DEBUG concluída!");
        Console.WriteLine("📝 Use os valores X acima para definir ColumnRanges\n");

      }
      catch (Exception ex)
      {
        Console.WriteLine($"❌ Erro: {ex.Message}");
        Console.WriteLine($"Stack: {ex.StackTrace}");
        throw;
      }

      return transactions;
    });
  }

  private class TextChunkEx
  {
    public string Text { get; set; } = "";
    public float X { get; set; }
    public float Y { get; set; }
  }

  private class LocationTextExtractionStrategyEx : LocationTextExtractionStrategy
  {
    private List<TextChunkEx> chunks = new List<TextChunkEx>();

    public override void RenderText(TextRenderInfo renderInfo)
    {
      base.RenderText(renderInfo);

      var bottomLeft = renderInfo.GetBaseline().GetStartPoint();
      var text = renderInfo.GetText();

      if (!string.IsNullOrWhiteSpace(text))
      {
        chunks.Add(new TextChunkEx
        {
          Text = text,
          X = bottomLeft[iTextSharp.text.pdf.parser.Vector.I1],
          Y = bottomLeft[iTextSharp.text.pdf.parser.Vector.I2]
        });
      }
    }

    public List<TextChunkEx> GetTextChunks()
    {
      return chunks;
    }
  }
}