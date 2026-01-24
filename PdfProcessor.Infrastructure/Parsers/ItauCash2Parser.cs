using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using ClosedXML.Excel;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using PdfProcessor.Core.Interfaces;
using PdfProcessor.Core.Models;

namespace PdfProcessor.Infrastructure.Parsers;

public class ItauCash2Parser : IItauCash2Parser
{
    private readonly ILogger<ItauCash2Parser> _logger;

    // ⚠️ AJUSTE ESTAS COORDENADAS conforme necessário após testar com seu PDF
    private class ColumnRanges
    {
        // Operation Date (primeira data)
        public float OperationDateStart = 40;
        public float OperationDateEnd = 95;

        // Value Date (segunda data)  
        public float ValueDateStart = 95;
        public float ValueDateEnd = 150;

        // Description (descrição)
        public float DescriptionStart = 150;
        public float DescriptionEnd = 400;

        // Value (valor da transação)
        public float ValueStart = 400;
        public float ValueEnd = 480;

        // Account Balance (saldo)
        public float BalanceStart = 480;
        public float BalanceEnd = 600;
    }

    // Classe para manter estado entre páginas
    private class ParserState
    {
        public string? CurrentAccount { get; set; }
        public ItauCash2Transaction? PendingTransaction { get; set; }
        public bool InTransactionsSection { get; set; }
    }

    public ItauCash2Parser(ILogger<ItauCash2Parser> logger)
    {
        _logger = logger;
    }

    public async Task<byte[]> ProcessBatchAsync(List<Stream> pdfStreams)
    {
        _logger.LogInformation($"🦁 Processando {pdfStreams.Count} PDF(s) Itaú Cash 2.0");

        var allAccountsData = new Dictionary<string, List<ItauCash2Transaction>>();

        for (int i = 0; i < pdfStreams.Count; i++)
        {
            _logger.LogInformation($"📄 Processando PDF {i + 1}/{pdfStreams.Count}");

            var accounts = await ExtractCashTransactionsAsync(pdfStreams[i]);

            foreach (var (accountId, transactions) in accounts)
            {
                if (!allAccountsData.ContainsKey(accountId))
                    allAccountsData[accountId] = new List<ItauCash2Transaction>();

                allAccountsData[accountId].AddRange(transactions);
                _logger.LogInformation($"  🦁 {accountId}: {transactions.Count} transação(ões)");
            }
        }

        _logger.LogInformation($"✅ Total: {allAccountsData.Count} conta(s), {allAccountsData.Sum(x => x.Value.Count)} transação(ões)");

        return await CreateExcelAsync(allAccountsData);
    }

    public async Task<Dictionary<string, List<ItauCash2Transaction>>> ExtractCashTransactionsAsync(Stream pdfStream)
    {
        return await Task.Run(() =>
        {
            var allAccounts = new Dictionary<string, List<ItauCash2Transaction>>();
            var state = new ParserState();

            pdfStream.Position = 0;
            using var pdfReader = new PdfReader(pdfStream);

            int totalPages = pdfReader.NumberOfPages;
            bool inCashSection = false;
            int startPage = 0;

            // PRIMEIRA PASSAGEM: Encontrar onde começa "Cash Transactions"
            for (int pageNum = 1; pageNum <= totalPages; pageNum++)
            {
                var text = PdfTextExtractor.GetTextFromPage(pdfReader, pageNum);

                if (text.Contains("Cash Transactions"))
                {
                    inCashSection = true;
                    startPage = pageNum;
                    _logger.LogInformation($"  📄 Cash Transactions encontrado na página {pageNum}");
                    break;
                }
            }

            if (!inCashSection)
            {
                _logger.LogWarning("  ⚠️ Nenhuma seção 'Cash Transactions' encontrada");
                return allAccounts;
            }

            // SEGUNDA PASSAGEM: Processar páginas consecutivas mantendo estado global
            for (int pageNum = startPage; pageNum <= totalPages; pageNum++)
            {
                var text = PdfTextExtractor.GetTextFromPage(pdfReader, pageNum);

                _logger.LogInformation($"  📄 Página {pageNum}/{totalPages}");

                // Verificar se ainda tem conteúdo relevante
                bool hasOpeningBalance = text.Contains("Opening Balance");
                bool hasClosingBalance = text.Contains("Closing Balance");
                bool hasDates = Regex.IsMatch(text, @"\d{2}/\d{2}/\d{4}");

                _logger.LogInformation($"    Opening: {hasOpeningBalance}, Closing: {hasClosingBalance}, Datas: {hasDates}");

                // Se não tem NADA relevante e já passou da primeira página, parar
                if (!hasOpeningBalance && !hasClosingBalance && !hasDates && pageNum > startPage)
                {
                    _logger.LogInformation($"  🏁 Fim da seção (sem conteúdo relevante)");
                    break;
                }

                // Processar página mantendo estado
                ProcessPage(pdfReader, pageNum, allAccounts, state);
            }

            // Finalizar transação pendente
            if (state.PendingTransaction != null && state.CurrentAccount != null)
            {
                if (!allAccounts.ContainsKey(state.CurrentAccount))
                    allAccounts[state.CurrentAccount] = new List<ItauCash2Transaction>();
                
                allAccounts[state.CurrentAccount].Add(state.PendingTransaction);
            }

            _logger.LogInformation($"  ✅ TOTAL FINAL:");
            foreach (var (accountId, transactions) in allAccounts)
            {
                _logger.LogInformation($"    🦁 {accountId}: {transactions.Count} transações");
            }

            return allAccounts;
        });
    }

    private void ProcessPage(
        PdfReader reader, 
        int pageNum, 
        Dictionary<string, List<ItauCash2Transaction>> allAccounts,
        ParserState state)
    {
        var columns = new ColumnRanges();

        try
        {
            var strategy = new LocationTextExtractionStrategyEx();
            PdfTextExtractor.GetTextFromPage(reader, pageNum, strategy);

            var chunks = strategy.GetTextChunks();

            var lineGroups = chunks
                .GroupBy(c => Math.Round(c.Y, 1))
                .OrderByDescending(g => g.Key)
                .ToList();

            foreach (var lineGroup in lineGroups)
            {
                var lineChunks = lineGroup.OrderBy(c => c.X).ToList();
                var lineText = string.Join("", lineChunks.Select(c => c.Text));

                // Detectar nova conta (Opening Balance)
                var openingMatch = Regex.Match(lineText, @"Opening Balance Account[:\s]*(\d+)\s*([A-Z]{3})", RegexOptions.IgnoreCase);
                if (openingMatch.Success)
                {
                    // Finalizar transação anterior
                    if (state.PendingTransaction != null && state.CurrentAccount != null)
                    {
                        if (!allAccounts.ContainsKey(state.CurrentAccount))
                            allAccounts[state.CurrentAccount] = new List<ItauCash2Transaction>();
                        
                        allAccounts[state.CurrentAccount].Add(state.PendingTransaction);
                        state.PendingTransaction = null;
                    }

                    var accountNumber = openingMatch.Groups[1].Value;
                    var currency = openingMatch.Groups[2].Value;
                    state.CurrentAccount = $"{accountNumber}_{currency}";
                    state.InTransactionsSection = true;

                    if (!allAccounts.ContainsKey(state.CurrentAccount))
                    {
                        allAccounts[state.CurrentAccount] = new List<ItauCash2Transaction>();
                        _logger.LogInformation($"    🦁 NOVA CONTA: {state.CurrentAccount}");
                    }
                    else
                    {
                        _logger.LogInformation($"    🔄 CONTINUANDO: {state.CurrentAccount}");
                    }

                    continue;
                }

                // Detectar fim (Closing Balance) - MAS NÃO RESETAR A CONTA!
                if (lineText.Contains("Closing Balance"))
                {
                    if (state.PendingTransaction != null && state.CurrentAccount != null)
                    {
                        if (!allAccounts.ContainsKey(state.CurrentAccount))
                            allAccounts[state.CurrentAccount] = new List<ItauCash2Transaction>();
                        
                        allAccounts[state.CurrentAccount].Add(state.PendingTransaction);
                        state.PendingTransaction = null;
                    }
                    
                    _logger.LogInformation($"    🏁 Closing Balance - {state.CurrentAccount} PERMANECE ATIVA");
                    // CRÍTICO: NÃO resetar state.CurrentAccount nem state.InTransactionsSection!
                    continue;
                }

                // Se não está em seção de transações OU não tem conta ativa, pular
                if (!state.InTransactionsSection && state.CurrentAccount == null)
                    continue;

                // Se tem conta ativa, processar mesmo sem InTransactionsSection (continuação)
                if (state.CurrentAccount == null)
                    continue;

                // Detectar transação (tem data)
                var operationDate = GetTextInRange(lineChunks, columns.OperationDateStart, columns.OperationDateEnd);

                if (!string.IsNullOrWhiteSpace(operationDate) && Regex.IsMatch(operationDate, @"\d{2}/\d{2}/\d{4}"))
                {
                    // Finalizar transação anterior
                    if (state.PendingTransaction != null)
                    {
                        allAccounts[state.CurrentAccount].Add(state.PendingTransaction);
                    }

                    // Extrair TODAS as colunas
                    var valueDate = GetTextInRange(lineChunks, columns.ValueDateStart, columns.ValueDateEnd).Trim();
                    var description = GetTextInRange(lineChunks, columns.DescriptionStart, columns.DescriptionEnd).Trim();
                    var value = GetTextInRange(lineChunks, columns.ValueStart, columns.ValueEnd).Trim();
                    var balance = GetTextInRange(lineChunks, columns.BalanceStart, columns.BalanceEnd).Trim();

                    // Se Value e Balance estão vazios, fallback
                    if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(balance))
                    {
                        var moneyValues = lineChunks
                            .Select(c => c.Text.Trim())
                            .Where(t => Regex.IsMatch(t, @"-?\d{1,3}(?:,\d{3})*\.\d{2}"))
                            .ToList();

                        if (moneyValues.Count >= 2)
                        {
                            value = moneyValues[moneyValues.Count - 2];
                            balance = moneyValues[moneyValues.Count - 1];
                        }
                    }

                    state.PendingTransaction = new ItauCash2Transaction
                    {
                        OperationDate = operationDate.Trim(),
                        ValueDate = valueDate,
                        Description = description,
                        Value = value,
                        AccountBalance = balance
                    };

                    if (string.IsNullOrWhiteSpace(state.PendingTransaction.ValueDate))
                        state.PendingTransaction.ValueDate = state.PendingTransaction.OperationDate;
                    
                    // Reativar seção
                    state.InTransactionsSection = true;
                }
                else if (state.PendingTransaction != null)
                {
                    // Linha de continuação (multiline description)
                    var descriptionExtra = GetTextInRange(lineChunks, columns.DescriptionStart, columns.DescriptionEnd);

                    if (!string.IsNullOrWhiteSpace(descriptionExtra))
                    {
                        state.PendingTransaction.Description += " " + descriptionExtra.Trim();
                    }
                }
            }

            // Log ao final da página
            if (state.CurrentAccount != null && allAccounts.ContainsKey(state.CurrentAccount))
            {
                _logger.LogInformation($"    📊 {state.CurrentAccount}: {allAccounts[state.CurrentAccount].Count} transações até agora");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"    ❌ Erro ao processar página {pageNum}");
        }
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

    private async Task<byte[]> CreateExcelAsync(Dictionary<string, List<ItauCash2Transaction>> accountsData)
    {
        return await Task.Run(() =>
        {
            _logger.LogInformation("📊 Criando arquivo Excel");

            using var workbook = new XLWorkbook();

            foreach (var (accountId, transactions) in accountsData)
            {
                if (transactions.Count == 0)
                    continue;

                var sheetName = accountId.Length > 31 ? accountId.Substring(0, 31) : accountId;
                var worksheet = workbook.Worksheets.Add(sheetName);

                // Headers
                worksheet.Cell(1, 1).Value = "Operation Date";
                worksheet.Cell(1, 2).Value = "Value Date";
                worksheet.Cell(1, 3).Value = "Description";
                worksheet.Cell(1, 4).Value = "Value";
                worksheet.Cell(1, 5).Value = "Account Balance";

                // Estilo do header
                var headerRange = worksheet.Range(1, 1, 1, 5);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

                // Dados
                for (int i = 0; i < transactions.Count; i++)
                {
                    var t = transactions[i];
                    worksheet.Cell(i + 2, 1).Value = t.OperationDate;
                    worksheet.Cell(i + 2, 2).Value = t.ValueDate;
                    worksheet.Cell(i + 2, 3).Value = t.Description;
                    worksheet.Cell(i + 2, 4).Value = t.Value;
                    worksheet.Cell(i + 2, 5).Value = t.AccountBalance;
                }

                // Auto-fit colunas
                worksheet.Columns().AdjustToContents();

                _logger.LogInformation($"  ✅ Aba '{sheetName}': {transactions.Count} transação(ões)");
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            _logger.LogInformation($"✅ Excel criado: {stream.Length} bytes");

            return stream.ToArray();
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