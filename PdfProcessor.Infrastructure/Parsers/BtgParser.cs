using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using PdfProcessor.Core.Interfaces;
using PdfProcessor.Core.Models;
using System.Text.RegularExpressions;

namespace PdfProcessor.Infrastructure.Parsers;

public class BtgParser : IBtgParser
{
    // ✅ POSIÇÕES CORRETAS (ajustadas manualmente)
    private class ColumnRanges
    {
        public float ProcessDateStart = 30;
        public float ProcessDateEnd = 74;
        
        public float TradeDateStart = 74;
        public float TradeDateEnd = 112;
        
        public float ActivityStart = 112;
        public float ActivityEnd = 252;
        
        public float DescriptionStart = 252;
        public float DescriptionEnd = 400;
        
        public float QuantityStart = 400;
        public float QuantityEnd = 465;
        
        public float PriceStart = 465;
        public float PriceEnd = 533;
        
        public float AccruedStart = 534;
        public float AccruedEnd = 640;
        
        public float AmountStart = 640;
        public float AmountEnd = 701;
        
        public float CurrencyStart = 701;
        public float CurrencyEnd = 800;
    }

    public async Task<List<BtgTransaction>> ParsePdfAsync(Stream pdfStream, string fileName)
    {
        var transactions = new List<BtgTransaction>();

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
                    var text = PdfTextExtractor.GetTextFromPage(pdfReader, pageNum);

                    if (!text.Contains("Transactions in Date Sequence"))
                    {
                        continue;
                    }

                    Console.WriteLine($"\n📄 === PÁGINA {pageNum} ===");
                    
                    var pageTransactions = ExtractTransactionsWithPosition(pdfReader, pageNum);
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

    private List<BtgTransaction> ExtractTransactionsWithPosition(PdfReader reader, int pageNum)
    {
        var transactions = new List<BtgTransaction>();
        var columns = new ColumnRanges();

        try
        {
            var strategy = new LocationTextExtractionStrategyEx();
            var pageText = PdfTextExtractor.GetTextFromPage(reader, pageNum, strategy);
            
            var chunks = strategy.GetTextChunks();

            // Agrupar por linha (Y)
            var lineGroups = chunks
                .GroupBy(c => Math.Round(c.Y, 1))
                .OrderByDescending(g => g.Key)
                .ToList();

            bool inTableSection = false;
            BtgTransaction? currentTransaction = null;

            foreach (var lineGroup in lineGroups)
            {
                var lineChunks = lineGroup.OrderBy(c => c.X).ToList();
                var lineText = string.Join("", lineChunks.Select(c => c.Text));

                // Detectar início
                if (lineText.Contains("Transactions in Date Sequence"))
                {
                    inTableSection = true;
                    continue;
                }

                // Detectar fim
                if (lineText.Contains("Total Value of Transactions"))
                {
                    break;
                }

                // Pular cabeçalhos
                if (lineText.Contains("Process") && lineText.Contains("Activity Type"))
                {
                    continue;
                }

                if (!inTableSection)
                {
                    continue;
                }

                // ✅ VERIFICAR SE É NOVA TRANSAÇÃO OU CONTINUAÇÃO
                var processDate = GetTextInRange(lineChunks, columns.ProcessDateStart, columns.ProcessDateEnd);
                
                // Se tem data = NOVA transação
                if (!string.IsNullOrWhiteSpace(processDate) && Regex.IsMatch(processDate, @"\d{2}/\d{2}/\d{2}"))
                {
                    // Salvar transação anterior se existir
                    if (currentTransaction != null)
                    {
                        transactions.Add(currentTransaction);
                    }
                    
                    // Criar nova transação
                    currentTransaction = new BtgTransaction();
                    currentTransaction.ProcessSettlementDate = processDate;
                    currentTransaction.TradeTransactionDate = GetTextInRange(lineChunks, columns.TradeDateStart, columns.TradeDateEnd);
                    currentTransaction.ActivityType = GetTextInRange(lineChunks, columns.ActivityStart, columns.ActivityEnd);
                    currentTransaction.Description = GetTextInRange(lineChunks, columns.DescriptionStart, columns.DescriptionEnd);
                    currentTransaction.Quantity = GetTextInRange(lineChunks, columns.QuantityStart, columns.QuantityEnd);
                    currentTransaction.Price = GetTextInRange(lineChunks, columns.PriceStart, columns.PriceEnd);
                    currentTransaction.AccruedInterest = GetTextInRange(lineChunks, columns.AccruedStart, columns.AccruedEnd);
                    currentTransaction.Amount = GetTextInRange(lineChunks, columns.AmountStart, columns.AmountEnd);
                    currentTransaction.Currency = GetTextInRange(lineChunks, columns.CurrencyStart, columns.CurrencyEnd);
                    
                    Console.WriteLine($"   ✓ Nova: {currentTransaction.ProcessSettlementDate} | {currentTransaction.ActivityType}");
                }
                // Se NÃO tem data = CONTINUAÇÃO da anterior
                else if (currentTransaction != null)
                {
                    var activityContinuation = GetTextInRange(lineChunks, columns.ActivityStart, columns.ActivityEnd);
                    var descriptionContinuation = GetTextInRange(lineChunks, columns.DescriptionStart, columns.DescriptionEnd);
                    
                    if (!string.IsNullOrWhiteSpace(activityContinuation))
                    {
                        currentTransaction.ActivityType += " " + activityContinuation;
                        Console.WriteLine($"     + Activity: {activityContinuation}");
                    }
                    
                    if (!string.IsNullOrWhiteSpace(descriptionContinuation))
                    {
                        currentTransaction.Description += " " + descriptionContinuation;
                        Console.WriteLine($"     + Description: {descriptionContinuation}");
                    }
                }
            }
            
            // ✅ Adicionar última transação
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