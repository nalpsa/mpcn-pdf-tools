using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using PdfProcessor.Core.Interfaces;
using PdfProcessor.Core.Models;
using System.Text.RegularExpressions;

namespace PdfProcessor.Infrastructure.Parsers;

public class MorganStanleyParser : IMorganStanleyParser
{
  // ✅ POSIÇÕES REAIS detectadas no DEBUG
  private class ColumnRanges
  {
    public float ActivityDateStart = 30;
    public float ActivityDateEnd = 75;

    public float SettlementDateStart = 75;
    public float SettlementDateEnd = 113;

    public float ActivityTypeStart = 113;
    public float ActivityTypeEnd = 210;

    public float DescriptionStart = 210;
    public float DescriptionEnd = 364;

    public float CommentsStart = 364;
    public float CommentsEnd = 567;

    public float QuantityStart = 567;
    public float QuantityEnd = 613;

    public float PriceStart = 613;
    public float PriceEnd = 700;

    public float CreditsDebitsStart = 720;
    public float CreditsDebitsEnd = 800;
  }

  public async Task<List<MorganStanleyTransaction>> ParsePdfAsync(Stream pdfStream, string fileName)
  {
    var transactions = new List<MorganStanleyTransaction>();

    return await Task.Run(() =>
    {
      try
      {
        Console.WriteLine($"📄 Processando {fileName}");

        pdfStream.Position = 0;
        using var pdfReader = new PdfReader(pdfStream);

        int totalPages = pdfReader.NumberOfPages;
        string currentAccountNumber = "";

        for (int pageNum = 1; pageNum <= totalPages; pageNum++)
        {
          var text = PdfTextExtractor.GetTextFromPage(pdfReader, pageNum);

          // ✅ DETECTAR ACCOUNT NUMBER
          var lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
          for (int i = 0; i < lines.Length; i++)
          {
            if (lines[i].Contains("Select UMA Active Assets Account") && i + 1 < lines.Length)
            {
              // Próxima linha tem o account number
              var accountLine = lines[i + 1];
              // Extrair número (formato: 442-084511-943)
              var match = Regex.Match(accountLine, @"(\d{3}-\d{6}-\d{3})");
              if (match.Success)
              {
                currentAccountNumber = match.Groups[1].Value;
                Console.WriteLine($"🏦 Account detectado: {currentAccountNumber}");
              }
              break;
            }
          }

          // ✅ VERIFICAR SE TEM TABELA
          if (!text.Contains("CASH FLOW ACTIVITY BY DATE"))
          {
            continue;
          }

          Console.WriteLine($"\n📄 === PÁGINA {pageNum} ===");

          var pageTransactions = ExtractTransactionsWithPosition(pdfReader, pageNum, currentAccountNumber);
          transactions.AddRange(pageTransactions);

          Console.WriteLine($"✅ {pageTransactions.Count} transação(ões) extraída(s)");
        }

        Console.WriteLine($"\n✅ Total: {transactions.Count} transação(ões)");
      }
      catch (Exception ex)
      {
        Console.WriteLine($"❌ Erro: {ex.Message}");
        throw;
      }

      return transactions;
    });
  }

  private List<MorganStanleyTransaction> ExtractTransactionsWithPosition(PdfReader reader, int pageNum, string accountNumber)
  {
    var transactions = new List<MorganStanleyTransaction>();
    var columns = new ColumnRanges();

    try
    {
      var strategy = new LocationTextExtractionStrategyEx();
      var pageText = PdfTextExtractor.GetTextFromPage(reader, pageNum, strategy);

      var chunks = strategy.GetTextChunks();

      var lineGroups = chunks
          .GroupBy(c => Math.Round(c.Y, 1))
          .OrderByDescending(g => g.Key)
          .ToList();

      bool inTableSection = false;
      MorganStanleyTransaction? currentTransaction = null;

      foreach (var lineGroup in lineGroups)
      {
        var lineChunks = lineGroup.OrderBy(c => c.X).ToList();
        var lineText = string.Join("", lineChunks.Select(c => c.Text));

        // Detectar início
        if (lineText.Contains("CASH FLOW ACTIVITY BY DATE"))
        {
          inTableSection = true;
          Console.WriteLine("✅ Início da tabela detectado");
          continue;
        }

        // Pular cabeçalho das colunas
        if (lineText.Contains("Activity") && lineText.Contains("Date") && lineText.Contains("Type"))
        {
          continue;
        }

        // Detectar fim
        if (lineText.Contains("NET CREDITS/(DEBITS)"))
        {
          if (currentTransaction != null)
          {
            transactions.Add(currentTransaction);
            currentTransaction = null;
          }
          Console.WriteLine("✅ Fim da tabela detectado");
          break;
        }

        if (!inTableSection)
        {
          continue;
        }

        // ✅ VERIFICAR SE É NOVA TRANSAÇÃO OU CONTINUAÇÃO
        var activityDate = GetTextInRange(lineChunks, columns.ActivityDateStart, columns.ActivityDateEnd);
        var settlementDate = GetTextInRange(lineChunks, columns.SettlementDateStart, columns.SettlementDateEnd);

        // Se TEM data (activity ou settlement) = NOVA transação
        bool hasDate = !string.IsNullOrWhiteSpace(activityDate) && Regex.IsMatch(activityDate, @"\d+/\d+");

        if (hasDate)
        {
          // Salvar transação anterior
          if (currentTransaction != null)
          {
            transactions.Add(currentTransaction);
          }

          // Criar nova transação
          currentTransaction = new MorganStanleyTransaction();
          currentTransaction.AccountNumber = accountNumber;
          currentTransaction.ActivityDate = activityDate;
          currentTransaction.SettlementDate = settlementDate;
          currentTransaction.ActivityType = GetTextInRange(lineChunks, columns.ActivityTypeStart, columns.ActivityTypeEnd);
          currentTransaction.Description = GetTextInRange(lineChunks, columns.DescriptionStart, columns.DescriptionEnd);
          currentTransaction.Comments = GetTextInRange(lineChunks, columns.CommentsStart, columns.CommentsEnd);
          currentTransaction.Quantity = GetTextInRange(lineChunks, columns.QuantityStart, columns.QuantityEnd);
          currentTransaction.Price = GetTextInRange(lineChunks, columns.PriceStart, columns.PriceEnd);
          currentTransaction.CreditsDebits = GetTextInRange(lineChunks, columns.CreditsDebitsStart, columns.CreditsDebitsEnd);

          Console.WriteLine($"   ✓ {currentTransaction.ActivityDate} | {currentTransaction.ActivityType}");
        }
        // Se NÃO tem data = CONTINUAÇÃO (multilinha)
        else if (currentTransaction != null)
        {
          var activityTypeExtra = GetTextInRange(lineChunks, columns.ActivityTypeStart, columns.ActivityTypeEnd);
          var descriptionExtra = GetTextInRange(lineChunks, columns.DescriptionStart, columns.DescriptionEnd);
          var commentsExtra = GetTextInRange(lineChunks, columns.CommentsStart, columns.CommentsEnd);

          if (!string.IsNullOrWhiteSpace(activityTypeExtra))
          {
            currentTransaction.ActivityType += " " + activityTypeExtra;
          }

          if (!string.IsNullOrWhiteSpace(descriptionExtra))
          {
            currentTransaction.Description += " " + descriptionExtra;
          }

          if (!string.IsNullOrWhiteSpace(commentsExtra))
          {
            currentTransaction.Comments += " " + commentsExtra;
          }
        }
      }

      // Adicionar última transação
      if (currentTransaction != null)
      {
        transactions.Add(currentTransaction);
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine($"❌ Erro ao processar página {pageNum}: {ex.Message}");
    }

    return transactions;
  }

  private string GetTextInRange(List<TextChunkEx> chunks, float startX, float endX)
  {
    var textsInRange = chunks
        .Where(c => c.X >= startX && c.X < endX)
        .OrderBy(c => c.X)
        .Select(c => c.Text)
        .ToList();

    return string.Join(" ", textsInRange).Trim();
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