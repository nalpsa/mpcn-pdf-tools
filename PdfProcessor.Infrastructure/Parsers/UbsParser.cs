using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using PdfProcessor.Core.Interfaces;
using PdfProcessor.Core.Models;
using System.Text.RegularExpressions;

namespace PdfProcessor.Infrastructure.Parsers;

public class UbsParser : IUbsParser
{
  // ✅ POSIÇÕES CORRETAS (ajustadas manualmente)
  private class ColumnRanges
  {
    public float DateStart = 40;
    public float DateEnd = 92;

    public float InformationStart = 92;
    public float InformationEnd = 210;

    public float DebitsStart = 210;
    public float DebitsEnd = 309;

    public float CreditsStart = 309;
    public float CreditsEnd = 391;

    public float ValueDateStart = 438;
    public float ValueDateEnd = 490;

    public float BalanceStart = 490;
    public float BalanceEnd = 600;
  }

  public async Task<List<UbsTransaction>> ParsePdfAsync(Stream pdfStream, string fileName)
  {
    var transactions = new List<UbsTransaction>();

    return await Task.Run(() =>
    {
      try
      {
        Console.WriteLine($"📄 Processando {fileName}");

        pdfStream.Position = 0;
        using var pdfReader = new PdfReader(pdfStream);

        int totalPages = pdfReader.NumberOfPages;

        for (int pageNum = 1; pageNum <= totalPages; pageNum++)
        {
          Console.WriteLine($"\n📄 === PÁGINA {pageNum} ===");

          var text = PdfTextExtractor.GetTextFromPage(pdfReader, pageNum);

          // ✅ DETECTAR IBAN - aceita com ou sem espaços
          string currentIban = "";

          var lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
          for (int i = 0; i < lines.Length; i++)
          {
            if (lines[i].Contains("UBS current account"))
            {
              // Procurar IBAN nas próximas 5 linhas
              for (int j = i + 1; j < Math.Min(i + 6, lines.Length); j++)
              {
                // ✅ REGEX FLEXÍVEL: aceita espaços opcionais
                // Padrão: IBAN CH + 2 dígitos + resto (com ou sem espaços)
                var ibanMatch = Regex.Match(lines[j], @"IBAN\s+(CH\d{2}[\s\d]{4,}[\dA-Z\s]+)");

                if (ibanMatch.Success)
                {
                  currentIban = ibanMatch.Groups[1].Value.Trim();
                  Console.WriteLine($"🏦 IBAN detectado: {currentIban}");
                  break;
                }

                // Fallback: pegar linha inteira se começa com CH
                if (lines[j].Trim().StartsWith("IBAN CH"))
                {
                  currentIban = lines[j].Replace("IBAN", "").Trim();
                  Console.WriteLine($"🏦 IBAN detectado (fallback): {currentIban}");
                  break;
                }
              }
              if (!string.IsNullOrWhiteSpace(currentIban)) break;
            }
          }

          if (!text.Contains("Account Statement"))
          {
            Console.WriteLine("⏭️ Página sem Account Statement");
            continue;
          }

          var pageTransactions = ExtractTransactionsWithPosition(pdfReader, pageNum, currentIban);
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

  private List<UbsTransaction> ExtractTransactionsWithPosition(PdfReader reader, int pageNum, string accountIban)
  {
    var transactions = new List<UbsTransaction>();
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
      UbsTransaction? currentTransaction = null;

      foreach (var lineGroup in lineGroups)
      {
        var lineChunks = lineGroup.OrderBy(c => c.X).ToList();
        var lineText = string.Join("", lineChunks.Select(c => c.Text));

        if (lineText.Contains("Date") && lineText.Contains("Information") && lineText.Contains("Balance"))
        {
          inTableSection = true;
          Console.WriteLine("✅ Início da tabela detectado");
          continue;
        }

        if (lineText.Contains("Closing balance") || lineText.Contains("Balance of closing"))
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

        var date = GetTextInRange(lineChunks, columns.DateStart, columns.DateEnd);

        if (!string.IsNullOrWhiteSpace(date) && Regex.IsMatch(date, @"\d{2}\.\d{2}\.\d{2}"))
        {
          if (currentTransaction != null)
          {
            transactions.Add(currentTransaction);
          }

          currentTransaction = new UbsTransaction();
          currentTransaction.AccountIban = accountIban;
          currentTransaction.Date = date;
          currentTransaction.Information = GetTextInRange(lineChunks, columns.InformationStart, columns.InformationEnd);
          currentTransaction.Debits = GetTextInRange(lineChunks, columns.DebitsStart, columns.DebitsEnd);
          currentTransaction.Credits = GetTextInRange(lineChunks, columns.CreditsStart, columns.CreditsEnd);
          currentTransaction.ValueDate = GetTextInRange(lineChunks, columns.ValueDateStart, columns.ValueDateEnd);
          currentTransaction.Balance = GetTextInRange(lineChunks, columns.BalanceStart, columns.BalanceEnd);

          Console.WriteLine($"   ✓ {currentTransaction.Date} | {currentTransaction.Information}");
        }
        else if (currentTransaction != null)
        {
          var informationExtra = GetTextInRange(lineChunks, columns.InformationStart, columns.InformationEnd);

          if (!string.IsNullOrWhiteSpace(informationExtra))
          {
            currentTransaction.Information += " " + informationExtra;
            Console.WriteLine($"     + Info: {informationExtra}");
          }
        }
      }

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