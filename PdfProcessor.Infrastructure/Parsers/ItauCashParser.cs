using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using ClosedXML.Excel;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using PdfProcessor.Core.Interfaces;
using PdfProcessor.Core.Models;

namespace PdfProcessor.Infrastructure.Parsers;

public class ItauCashParser : IItauCashParser
{
    private readonly ILogger<ItauCashParser> _logger;
    
    public ItauCashParser(ILogger<ItauCashParser> logger)
    {
        _logger = logger;
    }
    
    public async Task<byte[]> ProcessBatchAsync(List<Stream> pdfStreams)
    {
        _logger.LogInformation($"🏦 Processando {pdfStreams.Count} PDF(s) Itaú Cash");
        
        // 1. Extrair transações de cada PDF
        var allAccountsData = new Dictionary<string, List<ItauCashTransaction>>();
        
        for (int i = 0; i < pdfStreams.Count; i++)
        {
            _logger.LogInformation($"📄 Processando PDF {i + 1}/{pdfStreams.Count}");
            
            var accounts = await ExtractCashTransactionsAsync(pdfStreams[i]);
            
            // 2. Consolidar contas
            foreach (var (accountId, transactions) in accounts)
            {
                if (!allAccountsData.ContainsKey(accountId))
                    allAccountsData[accountId] = new List<ItauCashTransaction>();
                
                allAccountsData[accountId].AddRange(transactions);
                _logger.LogInformation($"  🏦 {accountId}: {transactions.Count} transação(ões)");
            }
        }
        
        _logger.LogInformation($"✅ Total: {allAccountsData.Count} conta(s), {allAccountsData.Sum(x => x.Value.Count)} transação(ões)");
        
        // 3. Criar Excel
        return await CreateExcelAsync(allAccountsData);
    }
    
    public async Task<Dictionary<string, List<ItauCashTransaction>>> ExtractCashTransactionsAsync(Stream pdfStream)
    {
        return await Task.Run(() =>
        {
            pdfStream.Position = 0;
            
            using var pdfReader = new PdfReader(pdfStream);
            using var pdfDocument = new PdfDocument(pdfReader);
            
            var fullText = new StringBuilder();
            
            for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
            {
                var page = pdfDocument.GetPage(i);
                var text = PdfTextExtractor.GetTextFromPage(page);
                fullText.AppendLine($"\n=== PÁGINA {i} ===\n");
                fullText.AppendLine(text);
                _logger.LogInformation($"  📄 Página {i}: {text?.Length ?? 0} caracteres");
            }
            
            // Parse do texto
            return ParseCashTransactions(fullText.ToString());
        });
    }
    
    private Dictionary<string, List<ItauCashTransaction>> ParseCashTransactions(string fullText)
    {
        _logger.LogInformation("🔍 Parseando Cash Transactions");
        
        var accountsData = new Dictionary<string, List<ItauCashTransaction>>();
        var lines = fullText.Split('\n');
        
        string? currentAccount = null;
        bool inCashTransactionsSection = false;
        bool inTransactionsTable = false;
        ItauCashTransaction? pendingTransaction = null;
        
        _logger.LogInformation($"📋 Total de linhas: {lines.Length}");
        
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            
            if (string.IsNullOrWhiteSpace(line))
                continue;
            
            // 1. DETECTAR SEÇÃO CASH TRANSACTIONS
            if (line.Contains("Cash Transactions") && !inCashTransactionsSection)
            {
                inCashTransactionsSection = true;
                _logger.LogInformation($"🎯 Seção Cash Transactions encontrada na linha {i}");
                continue;
            }
            
            // SÓ PROCESSAR SE ESTIVERMOS NA SEÇÃO CORRETA
            if (!inCashTransactionsSection)
                continue;
            
            // 2. DETECTAR OPENING BALANCE (NOVA CONTA)
            var openingPatterns = new[]
            {
                @"Opening Balance Account[:\s]*(\d+)\s*([A-Z]{3})",
                @"Opening Balance Account:\s*(\d+)\s*([A-Z]{3})",
                @"Opening Balance\s+Account:\s*(\d+)\s*([A-Z]{3})"
            };
            
            Match? openingMatch = null;
            foreach (var pattern in openingPatterns)
            {
                openingMatch = Regex.Match(line, pattern, RegexOptions.IgnoreCase);
                if (openingMatch.Success)
                {
                    // FINALIZAR CONTA ANTERIOR SE EXISTIR
                    if (pendingTransaction != null && currentAccount != null)
                    {
                        if (!accountsData.ContainsKey(currentAccount))
                            accountsData[currentAccount] = new List<ItauCashTransaction>();
                        
                        accountsData[currentAccount].Add(pendingTransaction);
                        _logger.LogInformation($"  ✅ Última transação da conta anterior finalizada");
                    }
                    
                    // INICIAR NOVA CONTA
                    var accountNumber = openingMatch.Groups[1].Value;
                    var currency = openingMatch.Groups[2].Value;
                    currentAccount = $"{accountNumber}_{currency}";
                    inTransactionsTable = true;
                    pendingTransaction = null;
                    
                    _logger.LogInformation($"🏦 NOVA CONTA: {currentAccount}");
                    
                    if (!accountsData.ContainsKey(currentAccount))
                        accountsData[currentAccount] = new List<ItauCashTransaction>();
                    
                    break;
                }
            }
            
            if (openingMatch?.Success == true)
                continue;
            
            // 3. DETECTAR CLOSING BALANCE (FIM DA CONTA ATUAL)
            if (line.Contains("Closing Balance") && inTransactionsTable && currentAccount != null)
            {
                _logger.LogInformation($"🏁 Closing Balance encontrado");
                
                // Finalizar transação pendente
                if (pendingTransaction != null)
                {
                    accountsData[currentAccount].Add(pendingTransaction);
                    _logger.LogInformation($"  ✅ Última transação finalizada");
                }
                
                _logger.LogInformation($"  📊 Total de transações para {currentAccount}: {accountsData[currentAccount].Count}");
                
                // RESETAR APENAS O ESTADO DA TABELA, NÃO DA SEÇÃO
                // (pode haver mais contas no mesmo PDF)
                inTransactionsTable = false;
                currentAccount = null;
                pendingTransaction = null;
                continue;
            }
            
            // 4. PROCESSAR TRANSAÇÃO
            if (inTransactionsTable && currentAccount != null)
            {
                var transaction = ParseTransactionLine(line, i);
                
                if (transaction != null)
                {
                    // Nova transação com data
                    if (pendingTransaction != null)
                    {
                        // Finalizar anterior
                        accountsData[currentAccount].Add(pendingTransaction);
                    }
                    
                    pendingTransaction = transaction;
                }
                else if (pendingTransaction != null && line.Length > 5)
                {
                    // Possível continuação da descrição
                    // Verificar se é realmente continuação (não tem valores grandes no final)
                    if (!Regex.IsMatch(line, @"\d{1,3}(?:,\d{3})*\.\d{2}$"))
                    {
                        pendingTransaction.Description += $" {line}";
                    }
                }
            }
        }
        
        // Finalizar última transação pendente
        if (pendingTransaction != null && currentAccount != null)
        {
            if (!accountsData.ContainsKey(currentAccount))
                accountsData[currentAccount] = new List<ItauCashTransaction>();
            
            accountsData[currentAccount].Add(pendingTransaction);
        }
        
        return accountsData;
    }
    
    private ItauCashTransaction? ParseTransactionLine(string line, int lineNum)
    {
        // Procurar datas no formato DD/MM/YYYY - OBRIGATÓRIO
        var datePattern = @"\b(\d{2}/\d{2}/\d{4})\b";
        var dates = Regex.Matches(line, datePattern);
        
        // Deve ter pelo menos uma data
        if (dates.Count < 1)
            return null;
        
        // FILTRAR cabeçalhos
        var headerIndicators = new[]
        {
            "Operation Date", "Value Date", "Description", "Value", "Account Balance",
            "Transactions", "Transaction", "PÁGINA", "===", "Closing Balance",
            "Opening Balance", "Portfolio", "Assets", "Future Cash Flow", "Benchmarks"
        };
        
        if (headerIndicators.Any(h => line.Contains(h, StringComparison.OrdinalIgnoreCase)))
            return null;
        
        // ESTRATÉGIA: IDENTIFICAR Value e Balance PELA POSIÇÃO NO FINAL DA LINHA
        // Padrão: DD/MM/YYYY DD/MM/YYYY Descrição... VALUE BALANCE
        // Os 2 ÚLTIMOS números grandes são sempre Value e Balance
        
        // Regex para capturar TODOS os números monetários
        var moneyPattern = @"-?\d{1,3}(?:,\d{3})*(?:\.\d{2})?";
        var moneyMatches = Regex.Matches(line, moneyPattern);
        
        // Deve ter pelo menos 2 valores: Value + Balance
        if (moneyMatches.Count < 2)
            return null;
        
        // PEGAR OS 2 ÚLTIMOS VALORES
        var value = moneyMatches[moneyMatches.Count - 2].Value;
        var balance = moneyMatches[moneyMatches.Count - 1].Value;
        
        // CONSTRUIR DESCRIÇÃO: remover datas e os 2 últimos valores
        var description = line;
        
        // 1. Remover datas
        foreach (Match dateMatch in dates)
        {
            description = description.Replace(dateMatch.Value, "");
        }
        
        // 2. Remover APENAS os valores identificados como Value/Balance
        var lastValueIndex = description.LastIndexOf(value);
        if (lastValueIndex != -1)
            description = description.Substring(0, lastValueIndex) + description.Substring(lastValueIndex + value.Length);
        
        var lastBalanceIndex = description.LastIndexOf(balance);
        if (lastBalanceIndex != -1)
            description = description.Substring(0, lastBalanceIndex) + description.Substring(lastBalanceIndex + balance.Length);
        
        // 3. Limpar espaços extras
        description = Regex.Replace(description, @"\s+", " ").Trim();
        
        // 4. Validação
        if (string.IsNullOrWhiteSpace(description) || description.Length < 3)
            description = "Transação";
        
        // Extrair datas
        var operationDate = dates[0].Value;
        var valueDate = dates.Count > 1 ? dates[1].Value : operationDate;
        
        return new ItauCashTransaction
        {
            OperationDate = operationDate,
            ValueDate = valueDate,
            Description = description,
            Value = value,
            AccountBalance = balance
        };
    }
    
    private async Task<byte[]> CreateExcelAsync(Dictionary<string, List<ItauCashTransaction>> accountsData)
    {
        return await Task.Run(() =>
        {
            _logger.LogInformation("📊 Criando arquivo Excel");
            
            using var workbook = new XLWorkbook();
            
            foreach (var (accountId, transactions) in accountsData)
            {
                if (transactions.Count == 0)
                    continue;
                
                // Nome da aba (máximo 31 caracteres)
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
}