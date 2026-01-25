using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using ClosedXML.Excel;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using PdfProcessor.Core.Interfaces;
using PdfProcessor.Core.Models;

namespace PdfProcessor.Infrastructure.Parsers;

public class JuliusBarParser : IJuliusBarParser
{
    private readonly ILogger<JuliusBarParser> _logger;

    // ⚠️ AJUSTE ESTAS COORDENADAS conforme necessário após testar com seu PDF
    private class ColumnRanges
    {
        // Trade Date / Value Date (mesma coluna)
        public float DateStart = 20;
        public float DateEnd = 80;

        // Type / Currency (mesma coluna, multiline)
        public float TypeStart = 80;
        public float TypeEnd = 200;

        // Quantity
        public float QuantityStart = 200;
        public float QuantityEnd = 280;

        // Details / ISIN (multiline)
        public float DetailsStart = 280;
        public float DetailsEnd = 700;

        // Amount (pegar primeiro valor)
        public float AmountStart = 700;
        public float AmountEnd = 820;

        // Exchange Rate (geralmente vazio)
        public float ExchangeRateStart = 820;
        public float ExchangeRateEnd = 950;
    }

    public JuliusBarParser(ILogger<JuliusBarParser> logger)
    {
        _logger = logger;
    }

    public async Task<byte[]> ProcessBatchAsync(List<Stream> pdfStreams)
    {
        _logger.LogInformation($"🏦 Processando {pdfStreams.Count} PDF(s) Julius Bär");

        var allAccountsData = new Dictionary<string, List<JuliusBarTransaction>>();

        for (int i = 0; i < pdfStreams.Count; i++)
        {
            _logger.LogInformation($"📄 Processando PDF {i + 1}/{pdfStreams.Count}");

            var accounts = await ExtractTransactionsAsync(pdfStreams[i]);

            foreach (var (accountId, transactions) in accounts)
            {
                if (!allAccountsData.ContainsKey(accountId))
                    allAccountsData[accountId] = new List<JuliusBarTransaction>();

                allAccountsData[accountId].AddRange(transactions);
                _logger.LogInformation($"  🏦 {accountId}: {transactions.Count} transação(ões)");
            }
        }

        _logger.LogInformation($"✅ Total: {allAccountsData.Count} conta(s), {allAccountsData.Sum(x => x.Value.Count)} transação(ões)");

        return await CreateExcelAsync(allAccountsData);
    }

    public async Task<Dictionary<string, List<JuliusBarTransaction>>> ExtractTransactionsAsync(Stream pdfStream)
    {
        return await Task.Run(() =>
        {
            var allAccounts = new Dictionary<string, List<JuliusBarTransaction>>();

            pdfStream.Position = 0;
            using var pdfReader = new PdfReader(pdfStream);
            using var pdfDocument = new PdfDocument(pdfReader);

            int totalPages = pdfDocument.GetNumberOfPages();
            bool foundSection = false;
            int startPage = 0;

            // PRIMEIRA PASSAGEM: Encontrar onde começa a seção
            for (int pageNum = 1; pageNum <= totalPages; pageNum++)
            {
                var page = pdfDocument.GetPage(pageNum);
                var text = PdfTextExtractor.GetTextFromPage(page);

                _logger.LogInformation($"  📄 Página {pageNum} ({text.Length} chars)");
                
                // Mostrar primeiras 500 chars para debug
                if (text.Length > 0)
                {
                    _logger.LogInformation($"  Preview: {text.Substring(0, Math.Min(500, text.Length))}");
                }

                // Buscar variações possíveis
                var searchTerms = new[] 
                { 
                    "Account transactions", 
                    "Account Transactions", 
                    "ACCOUNT TRANSACTIONS",
                    "Account Balance",
                    "Trade Date",
                    "Value Date",
                    "Reporting Currency"
                };

                foreach (var term in searchTerms)
                {
                    if (text.Contains(term, StringComparison.OrdinalIgnoreCase))
                    {
                        foundSection = true;
                        startPage = pageNum;
                        _logger.LogInformation($"  ✅ '{term}' encontrado na página {pageNum}!");
                        break;
                    }
                }

                if (foundSection)
                    break;
            }

            if (!foundSection)
            {
                _logger.LogWarning("  ⚠️ Nenhuma seção relevante encontrada");
                return allAccounts;
            }

            _logger.LogInformation($"  📊 Processando a partir da página {startPage}");

            // SEGUNDA PASSAGEM: Processar páginas consecutivas
            var state = new ParserState();
            
            for (int pageNum = startPage; pageNum <= totalPages; pageNum++)
            {
                var page = pdfDocument.GetPage(pageNum);
                var text = PdfTextExtractor.GetTextFromPage(page);

                _logger.LogInformation($"  📄 Processando página {pageNum}");

                // Verificar se ainda tem conteúdo relevante
                bool hasAccountBalance = text.Contains("Account Balance", StringComparison.OrdinalIgnoreCase);
                bool hasBalanceAsOf = text.Contains("Balance as of", StringComparison.OrdinalIgnoreCase);
                bool hasDates = Regex.IsMatch(text, @"\d{2}\.\d{2}\.\d{4}");

                _logger.LogInformation($"    Account Balance: {hasAccountBalance}, Balance as of: {hasBalanceAsOf}, Datas: {hasDates}");

                // Mostrar mais contexto da página
                if (text.Length > 500)
                {
                    _logger.LogInformation($"    Texto completo ({text.Length} chars):");
                    _logger.LogInformation(text);
                }

                // Se não tem NADA relevante, parar
                if (!hasAccountBalance && !hasBalanceAsOf && !hasDates && pageNum > startPage)
                {
                    _logger.LogInformation($"  🏁 Fim da seção (sem conteúdo relevante)");
                    break;
                }

                // Processar página
                ProcessPage(page, pageNum, allAccounts, state);
            }

            // Finalizar transação pendente
            if (state.PendingTransaction != null && state.CurrentAccount != null)
            {
                if (!allAccounts.ContainsKey(state.CurrentAccount))
                    allAccounts[state.CurrentAccount] = new List<JuliusBarTransaction>();
                
                allAccounts[state.CurrentAccount].Add(state.PendingTransaction);
            }

            _logger.LogInformation($"  ✅ TOTAL FINAL:");
            foreach (var (accountId, transactions) in allAccounts)
            {
                _logger.LogInformation($"    🏦 {accountId}: {transactions.Count} transações");
            }

            return allAccounts;
        });
    }

    // Classe para manter estado entre páginas
    private class ParserState
    {
        public string? CurrentAccount { get; set; }
        public JuliusBarTransaction? PendingTransaction { get; set; }
        public bool InTransactionsSection { get; set; }
        public bool IsSecondLine { get; set; }
    }

    private void ProcessPage(
        PdfPage page,
        int pageNum,
        Dictionary<string, List<JuliusBarTransaction>> allAccounts,
        ParserState state)
    {
        try
        {
            var simpleText = PdfTextExtractor.GetTextFromPage(page);
            var lines = simpleText.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            _logger.LogInformation($"    📝 Processando {lines.Length} linhas");

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (line.Length == 0) continue;

                // Detectar nova conta
                var accountMatch = Regex.Match(line, @"Account Balance (MC\S+)\s*-\s*([A-Z]{3})\s+as of");
                if (accountMatch.Success)
                {
                    if (state.PendingTransaction != null && state.CurrentAccount != null)
                    {
                        if (!allAccounts.ContainsKey(state.CurrentAccount))
                            allAccounts[state.CurrentAccount] = new List<JuliusBarTransaction>();
                        
                        allAccounts[state.CurrentAccount].Add(state.PendingTransaction);
                        state.PendingTransaction = null;
                    }

                    var accountNumber = accountMatch.Groups[1].Value;
                    var currency = accountMatch.Groups[2].Value;
                    state.CurrentAccount = $"{accountNumber}_{currency}";
                    state.InTransactionsSection = true;
                    state.IsSecondLine = false;

                    if (!allAccounts.ContainsKey(state.CurrentAccount))
                    {
                        allAccounts[state.CurrentAccount] = new List<JuliusBarTransaction>();
                        _logger.LogInformation($"      🏦 NOVA CONTA: {state.CurrentAccount}");
                    }

                    continue;
                }

                // Detectar fim
                if (line.Contains("Balance as of", StringComparison.OrdinalIgnoreCase))
                {
                    if (state.PendingTransaction != null && state.CurrentAccount != null)
                    {
                        if (!allAccounts.ContainsKey(state.CurrentAccount))
                            allAccounts[state.CurrentAccount] = new List<JuliusBarTransaction>();
                        
                        allAccounts[state.CurrentAccount].Add(state.PendingTransaction);
                        state.PendingTransaction = null;
                    }
                    
                    _logger.LogInformation($"      🏁 Balance as of encontrado");
                    state.IsSecondLine = false;
                    continue;
                }

                // Pular headers e linhas irrelevantes
                if (line.Contains("Trade Date") || line.Contains("Value Date") || 
                    line.Contains("Interim Balance") || line.Contains("Type") ||
                    line.Contains("Currency") || line.Contains("ISIN") ||
                    line.Contains("Exchange") || line.Contains("Rate") ||
                    line.Contains("Reporting Currency") || line.Contains("Total"))
                {
                    continue;
                }

                if (state.CurrentAccount == null) continue;

                // LINHA 1: Trade Date (DD.MM.YYYY no início)
                if (Regex.IsMatch(line, @"^\d{2}\.\d{2}\.\d{4}") && !state.IsSecondLine)
                {
                    // Finalizar transação anterior
                    if (state.PendingTransaction != null)
                    {
                        allAccounts[state.CurrentAccount].Add(state.PendingTransaction);
                    }

                    // Parse usando regex para extrair campos específicos
                    // Formato: DD.MM.YYYY Type Quantity Details Amount Amount
                    
                    var match = Regex.Match(line, @"^(\d{2}\.\d{2}\.\d{4})\s+(.+)");
                    if (match.Success)
                    {
                        string tradeDate = match.Groups[1].Value;
                        string restOfLine = match.Groups[2].Value;

                        // Extrair todos os valores monetários (números com vírgulas e ponto decimal)
                        var moneyPattern = @"-?\d{1,3}(?:,\d{3})*\.\d{2}";
                        var moneyMatches = Regex.Matches(restOfLine, moneyPattern);
                        var moneyValues = moneyMatches.Select(m => m.Value).ToList();

                        // Remover valores monetários da string para pegar Type e Details
                        var textOnly = Regex.Replace(restOfLine, moneyPattern, "|||").Trim();
                        var textParts = textOnly.Split(new[] { "|||" }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(p => p.Trim())
                            .Where(p => p.Length > 0)
                            .ToList();

                        // Primeira parte é Type
                        string type = textParts.FirstOrDefault() ?? "";
                        
                        // Quantidade: primeiro valor monetário OU primeiro número sem negativos
                        string quantity = "";
                        if (moneyValues.Count > 0 && !moneyValues[0].StartsWith("-"))
                        {
                            quantity = moneyValues[0];
                            moneyValues.RemoveAt(0);
                        }

                        // Details: texto entre Type e valores finais
                        string details = textParts.Count > 1 ? string.Join(" ", textParts.Skip(1)) : "";

                        // Amount: último valor (ou penúltimo se houver 2+)
                        string amount = moneyValues.LastOrDefault() ?? "";

                        state.PendingTransaction = new JuliusBarTransaction
                        {
                            TradeDate = tradeDate,
                            Type = type,
                            Quantity = quantity,
                            Details = details,
                            Amount = amount,
                            ExchangeRate = "",
                            ReportingCurrency = "USD"
                        };

                        state.IsSecondLine = true;
                        
                        _logger.LogInformation($"      ✅ Linha 1: {tradeDate} | Type: {type} | Qty: {quantity} | Amount: {amount}");
                    }
                }
                // LINHA 2: Value Date (DD.MM.YYYY no início E IsSecondLine)
                else if (Regex.IsMatch(line, @"^\d{2}\.\d{2}\.\d{4}") && state.IsSecondLine && state.PendingTransaction != null)
                {
                    // Formato: DD.MM.YYYY Currency ISIN/Details
                    var match = Regex.Match(line, @"^(\d{2}\.\d{2}\.\d{4})\s+([A-Z]{3})\s*(.*)");
                    if (match.Success)
                    {
                        string currency = match.Groups[2].Value;
                        string isinAndDetails = match.Groups[3].Value.Trim();
                        
                        // Append Currency ao Type
                        state.PendingTransaction.Type += " " + currency;
                        
                        // Append ISIN/Details
                        if (!string.IsNullOrWhiteSpace(isinAndDetails))
                        {
                            state.PendingTransaction.Details += " " + isinAndDetails;
                        }
                        
                        _logger.LogInformation($"      ✅ Linha 2: Currency: {currency} | ISIN: {isinAndDetails}");
                    }

                    state.IsSecondLine = false;
                }
                // Continuação (não começa com data)
                else if (state.PendingTransaction != null && !line.StartsWith("0") && !line.StartsWith("1") && !line.StartsWith("2") && !line.StartsWith("3"))
                {
                    // Adicionar à descrição
                    if (state.IsSecondLine)
                    {
                        // Ainda na linha 2, pode ser ISIN quebrado
                        state.PendingTransaction.Details += " " + line;
                    }
                    else
                    {
                        // Linha extra de detalhes
                        state.PendingTransaction.Details += " " + line;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"    ❌ Erro ao processar página {pageNum}");
        }
    }

    private async Task<byte[]> CreateExcelAsync(Dictionary<string, List<JuliusBarTransaction>> accountsData)
    {
        return await Task.Run(() =>
        {
            _logger.LogInformation("📊 Criando arquivo Excel");

            using var workbook = new XLWorkbook();

            // Verificar se há dados
            if (accountsData.Count == 0 || accountsData.All(x => x.Value.Count == 0))
            {
                _logger.LogWarning("⚠️ Nenhum dado encontrado, criando Excel vazio com mensagem");
                
                // Criar aba com mensagem de aviso
                var ws = workbook.Worksheets.Add("Aviso");
                ws.Cell(1, 1).Value = "Nenhuma transação encontrada";
                ws.Cell(2, 1).Value = "Verifique se o PDF contém a seção 'Account transactions'";
                ws.Cell(3, 1).Value = "e se as contas seguem o padrão 'Account Balance MC...'";
                
                ws.Range(1, 1, 3, 1).Style.Fill.BackgroundColor = XLColor.LightYellow;
                ws.Columns().AdjustToContents();
            }
            else
            {
                foreach (var (accountId, transactions) in accountsData)
                {
                    if (transactions.Count == 0)
                        continue;

                    var sheetName = accountId.Length > 31 ? accountId.Substring(0, 31) : accountId;
                    var worksheet = workbook.Worksheets.Add(sheetName);

                    // Headers
                    worksheet.Cell(1, 1).Value = "Trade Date";
                    worksheet.Cell(1, 2).Value = "Type";
                    worksheet.Cell(1, 3).Value = "Quantity";
                    worksheet.Cell(1, 4).Value = "Details";
                    worksheet.Cell(1, 5).Value = "Amount";
                    worksheet.Cell(1, 6).Value = "Exchange Rate";
                    worksheet.Cell(1, 7).Value = "Reporting Currency";

                    // Estilo do header
                    var headerRange = worksheet.Range(1, 1, 1, 7);
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

                    // Dados
                    for (int i = 0; i < transactions.Count; i++)
                    {
                        var t = transactions[i];
                        worksheet.Cell(i + 2, 1).Value = t.TradeDate;
                        worksheet.Cell(i + 2, 2).Value = t.Type;
                        worksheet.Cell(i + 2, 3).Value = t.Quantity;
                        worksheet.Cell(i + 2, 4).Value = t.Details;
                        worksheet.Cell(i + 2, 5).Value = t.Amount;
                        worksheet.Cell(i + 2, 6).Value = t.ExchangeRate;
                        worksheet.Cell(i + 2, 7).Value = t.ReportingCurrency;
                    }

                    // Auto-fit colunas
                    worksheet.Columns().AdjustToContents();

                    _logger.LogInformation($"  ✅ Aba '{sheetName}': {transactions.Count} transação(ões)");
                }
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            _logger.LogInformation($"✅ Excel criado: {stream.Length} bytes");

            return stream.ToArray();
        });
    }
}