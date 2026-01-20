using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using PdfProcessor.Core.Interfaces;
using PdfProcessor.Core.Models;
using System.Text.RegularExpressions;

namespace PdfProcessor.Infrastructure.Parsers;

public class BtgParser : IBtgParser
{
    public async Task<List<BtgTransaction>> ParsePdfAsync(Stream pdfStream, string fileName)
    {
        var transactions = new List<BtgTransaction>();

        return await Task.Run(() =>
        {
            try
            {
                Console.WriteLine($"📄 Processando {fileName} usando iTextSharp");
                
                pdfStream.Position = 0;
                
                // ✅ iTextSharp 5.x suporta RC4 40-bit!
                using var pdfReader = new PdfReader(pdfStream);
                
                int totalPages = pdfReader.NumberOfPages;
                Console.WriteLine($"📄 {totalPages} página(s)");

                for (int pageNum = 1; pageNum <= totalPages; pageNum++)
                {
                    // iTextSharp usa índice 1-based
                    var text = PdfTextExtractor.GetTextFromPage(pdfReader, pageNum);

                    Console.WriteLine($"\n📄 === PÁGINA {pageNum} ===");
                    Console.WriteLine($"📝 {text.Length} caracteres");

                    if (!text.Contains("Transactions in Date Sequence"))
                    {
                        Console.WriteLine("⏭️ Página não contém tabela de transações");
                        continue;
                    }

                    var cleanedText = RemoveFooter(text);
                    var pageTransactions = ExtractTransactionsFromPage(cleanedText, pageNum);
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

    private string RemoveFooter(string pageText)
    {
        var pageMatch = Regex.Match(pageText, @"Page\s+\d+\s+of\s+\d+", RegexOptions.IgnoreCase);
        if (pageMatch.Success)
        {
            pageText = pageText.Substring(0, pageMatch.Index);
            Console.WriteLine("🗑️ Rodapé removido");
        }
        return pageText;
    }

    private List<BtgTransaction> ExtractTransactionsFromPage(string pageText, int pageNum)
    {
        var transactions = new List<BtgTransaction>();

        try
        {
            var startMatch = Regex.Match(pageText, @"Transactions in Date Sequence(?:\s*\(continued\))?", RegexOptions.IgnoreCase);
            if (!startMatch.Success)
            {
                Console.WriteLine("⚠️ Início da tabela não encontrado");
                return transactions;
            }

            var endMatch = Regex.Match(pageText, @"Total Value of Transactions", RegexOptions.IgnoreCase);
            
            int startIndex = startMatch.Index + startMatch.Length;
            int endIndex = endMatch.Success ? endMatch.Index : pageText.Length;

            string tableContent = pageText.Substring(startIndex, endIndex - startIndex);

            // Remover cabeçalho
            tableContent = Regex.Replace(tableContent, 
                @"Process/\s*Settlement\s*Date\s*Trade/\s*Transaction\s*Date\s*Activity Type\s*Description\s*Quantity\s*Price\s*Accrued Interest\s*Amount\s*Currency", 
                "", 
                RegexOptions.IgnoreCase);

            var lines = tableContent.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                                   .Select(l => l.Trim())
                                   .Where(l => !string.IsNullOrWhiteSpace(l))
                                   .ToList();

            Console.WriteLine($"📋 {lines.Count} linhas para processar");

            // ✅ AGRUPAR LINHAS MULTILINHAS
            var groupedLines = new List<string>();
            
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                
                // Se linha começa com data, é início de nova transação
                if (Regex.IsMatch(line, @"^\d{2}/\d{2}/\d{2}"))
                {
                    var fullLine = line;
                    
                    // Juntar linhas seguintes que NÃO começam com data
                    int j = i + 1;
                    while (j < lines.Count && !Regex.IsMatch(lines[j], @"^\d{2}/\d{2}/\d{2}"))
                    {
                        fullLine += " " + lines[j];
                        j++;
                    }
                    
                    groupedLines.Add(fullLine);
                    
                    // Pular linhas já processadas
                    i = j - 1;
                }
            }

            Console.WriteLine($"📦 {groupedLines.Count} transação(ões) agrupadas");

            // Processar cada transação agrupada
            foreach (var groupedLine in groupedLines)
            {
                var transaction = ParseTransactionLine(groupedLine);
                if (transaction != null)
                {
                    transactions.Add(transaction);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Erro ao processar página {pageNum}: {ex.Message}");
        }

        return transactions;
    }

    private BtgTransaction? ParseTransactionLine(string line)
    {
        try
        {
            var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 4)
            {
                return null;
            }

            var transaction = new BtgTransaction();
            transaction.ProcessSettlementDate = parts[0];

            int currentIndex = 1;

            if (currentIndex < parts.Length && Regex.IsMatch(parts[currentIndex], @"^\d{2}/\d{2}/\d{2}"))
            {
                transaction.TradeTransactionDate = parts[currentIndex];
                currentIndex++;
            }

            var activityParts = new List<string>();
            while (currentIndex < parts.Length && 
                   parts[currentIndex].All(c => char.IsUpper(c) || char.IsWhiteSpace(c) || c == '/') &&
                   !Regex.IsMatch(parts[currentIndex], @"^-?\d"))
            {
                activityParts.Add(parts[currentIndex]);
                currentIndex++;
                if (activityParts.Count >= 4) break;
            }
            transaction.ActivityType = string.Join(" ", activityParts);

            var descriptionParts = new List<string>();
            while (currentIndex < parts.Length &&
                   !IsNumeric(parts[currentIndex]) &&
                   parts[currentIndex] != "USD" &&
                   parts[currentIndex] != "BRL")
            {
                descriptionParts.Add(parts[currentIndex]);
                currentIndex++;
            }
            transaction.Description = string.Join(" ", descriptionParts);

            var remainingParts = parts.Skip(currentIndex).ToList();

            if (remainingParts.Count > 0 && Regex.IsMatch(remainingParts[^1], @"^[A-Z]{3}$"))
            {
                transaction.Currency = remainingParts[^1];
                remainingParts.RemoveAt(remainingParts.Count - 1);
            }

            if (remainingParts.Count > 0 && IsNumeric(remainingParts[^1]))
            {
                transaction.Amount = remainingParts[^1];
                remainingParts.RemoveAt(remainingParts.Count - 1);
            }

            if (remainingParts.Count > 0 && IsNumeric(remainingParts[^1]))
            {
                transaction.AccruedInterest = remainingParts[^1];
                remainingParts.RemoveAt(remainingParts.Count - 1);
            }

            if (remainingParts.Count > 0 && IsNumeric(remainingParts[^1]))
            {
                transaction.Price = remainingParts[^1];
                remainingParts.RemoveAt(remainingParts.Count - 1);
            }

            if (remainingParts.Count > 0 && IsNumeric(remainingParts[^1]))
            {
                transaction.Quantity = remainingParts[^1];
            }

            return transaction;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Erro ao parsear linha: {ex.Message}");
            return null;
        }
    }

    private bool IsNumeric(string value)
    {
        return Regex.IsMatch(value, @"^-?\d[\d,\.]*$");
    }
}