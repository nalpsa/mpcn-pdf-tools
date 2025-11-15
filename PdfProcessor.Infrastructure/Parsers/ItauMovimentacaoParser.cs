using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using ClosedXML.Excel;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using PdfProcessor.Core.Interfaces;
using PdfProcessor.Core.Models;

namespace PdfProcessor.Infrastructure.Parsers;

public class ItauMovimentacaoParser : IItauMovimentacaoParser
{
    private readonly ILogger<ItauMovimentacaoParser> _logger;
    
    public ItauMovimentacaoParser(ILogger<ItauMovimentacaoParser> logger)
    {
        _logger = logger;
    }
    
    public async Task<byte[]> ProcessBatchAsync(List<Stream> pdfStreams)
    {
        _logger.LogInformation($"💳 Processando {pdfStreams.Count} PDF(s)");
        
        // 1. Extrair transações de cada PDF
        var allAccountsData = new Dictionary<string, List<ItauTransaction>>();
        
        for (int i = 0; i < pdfStreams.Count; i++)
        {
            _logger.LogInformation($"📄 Processando PDF {i + 1}/{pdfStreams.Count}");
            
            var accounts = await ExtractMovimentacaoAsync(pdfStreams[i]);
            
            // 2. Consolidar contas
            foreach (var (accountId, transactions) in accounts)
            {
                if (!allAccountsData.ContainsKey(accountId))
                    allAccountsData[accountId] = new List<ItauTransaction>();
                
                allAccountsData[accountId].AddRange(transactions);
                _logger.LogInformation($"  🏦 {accountId}: {transactions.Count} transação(ões)");
            }
        }
        
        _logger.LogInformation($"✅ Total: {allAccountsData.Count} conta(s), {allAccountsData.Sum(x => x.Value.Count)} transação(ões)");
        
        // 3. Criar Excel
        return await CreateExcelAsync(allAccountsData);
    }
    
    public async Task<Dictionary<string, List<ItauTransaction>>> ExtractMovimentacaoAsync(Stream pdfStream)
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
            return ParseMovimentacao(fullText.ToString());
        });
    }
    
    private Dictionary<string, List<ItauTransaction>> ParseMovimentacao(string fullText)
    {
        _logger.LogInformation("🔍 Parseando movimentação bancária");
        
        var accounts = new Dictionary<string, List<ItauTransaction>>();
        var lines = fullText.Split('\n');
        
        string? currentAccount = null;
        bool inMovimentacaoSection = false;
        bool inTransactionsTable = false;
        string? currentDate = null;
        
        _logger.LogInformation($"📋 Total de linhas: {lines.Length}");
        
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            
            if (string.IsNullOrWhiteSpace(line))
                continue;
            
            // 1. Detectar agência e conta
            var accountMatch = Regex.Match(line, @"ag\s+(\d+)\s+cc\s+([0-9\-]+)", RegexOptions.IgnoreCase);
            if (accountMatch.Success)
            {
                var agencia = accountMatch.Groups[1].Value;
                var conta = accountMatch.Groups[2].Value;
                currentAccount = $"AG {agencia} - CC {conta}";
                
                if (!accounts.ContainsKey(currentAccount))
                    accounts[currentAccount] = new List<ItauTransaction>();
                
                _logger.LogInformation($"🏦 Conta detectada: {currentAccount}");
                continue;
            }
            
            // 2. Detectar início da seção movimentação
            if (line.Contains("Conta Corrente") && line.Contains("Movimentação"))
            {
                inMovimentacaoSection = true;
                _logger.LogInformation("📊 Seção de movimentação encontrada");
                continue;
            }
            
            // 3. Detectar início da tabela
            if (inMovimentacaoSection && 
                line.Contains("data", StringComparison.OrdinalIgnoreCase) && 
                line.Contains("descrição", StringComparison.OrdinalIgnoreCase))
            {
                inTransactionsTable = true;
                _logger.LogInformation("📋 Início da tabela de transações");
                continue;
            }
            
            // 4. Parse de transações
            if (inTransactionsTable && currentAccount != null)
            {
                var transaction = ParseTransactionLine(line, ref currentDate, i + 1);
                
                if (transaction != null)
                {
                    accounts[currentAccount].Add(transaction);
                }
            }
            
            // 5. Detectar fim da tabela
            if (line.Contains("totalizador", StringComparison.OrdinalIgnoreCase) || 
                line.Contains("Saldo final", StringComparison.OrdinalIgnoreCase))
            {
                inTransactionsTable = false;
                inMovimentacaoSection = false;
                _logger.LogInformation("🏁 Fim da tabela de transações");
            }
        }
        
        return accounts;
    }
    
    private ItauTransaction? ParseTransactionLine(string line, ref string? currentDate, int lineNumber)
    {
        // Filtros de linhas a ignorar
        var ignorePatterns = new[]
        {
            "Saldo Aplic Aut Mais",
            "totalizador",
            "Saldo final",
            "===",
            "PÁGINA",
            "data.*descrição",
            "Para demais siglas"
        };
        
        if (ignorePatterns.Any(p => Regex.IsMatch(line, p, RegexOptions.IgnoreCase)))
            return null;
        
        // 1. Extrair data (dd/MM)
        var dateMatch = Regex.Match(line, @"^(\d{1,2}/\d{1,2})\s*(.*)$");
        string data;
        string remainingText;
        
        if (dateMatch.Success)
        {
            data = dateMatch.Groups[1].Value;
            currentDate = data; // Salvar última data válida
            remainingText = dateMatch.Groups[2].Value.Trim();
        }
        else
        {
            data = currentDate ?? "";
            remainingText = line.Trim();
        }
        
        // 2. Encontrar valores monetários com suas posições
        var moneyMatches = Regex.Matches(line, @"\d{1,3}(?:\.\d{3})*,\d{2}[-]?");
        
        if (moneyMatches.Count == 0)
            return null;
        
        // 3. Classificar valores (entrada, saída, saldo)
        string entrada = "";
        string saida = "";
        string saldo = "";
        
        if (moneyMatches.Count == 1)
        {
            var value = moneyMatches[0].Value;
            var position = moneyMatches[0].Index;
            
            // Se está muito à direita (posição > 100), provavelmente é saldo
            if (position > 100)
            {
                saldo = value;
            }
            else
            {
                if (value.EndsWith('-'))
                    saida = value;
                else
                    entrada = value;
            }
        }
        else if (moneyMatches.Count == 2)
        {
            // O valor mais à direita é sempre o saldo
            saldo = moneyMatches[1].Value;
            
            if (moneyMatches[0].Value.EndsWith('-'))
                saida = moneyMatches[0].Value;
            else
                entrada = moneyMatches[0].Value;
        }
        else if (moneyMatches.Count >= 3)
        {
            // Último valor é sempre saldo
            saldo = moneyMatches[moneyMatches.Count - 1].Value;
            
            // Analisar valores anteriores
            for (int i = 0; i < moneyMatches.Count - 1; i++)
            {
                var value = moneyMatches[i].Value;
                if (value.EndsWith('-'))
                {
                    if (string.IsNullOrEmpty(saida))
                        saida = value;
                }
                else
                {
                    if (string.IsNullOrEmpty(entrada))
                        entrada = value;
                }
            }
        }
        
        // 4. Extrair descrição (remover valores)
        var descricao = remainingText;
        foreach (Match match in moneyMatches)
        {
            descricao = descricao.Replace(match.Value, "");
        }
        descricao = Regex.Replace(descricao, @"\s+", " ").Trim();
        
        // 5. Validações
        if (string.IsNullOrWhiteSpace(descricao) || descricao.Length < 2)
            return null;
        
        // Filtros específicos
        if (descricao.Contains("Saldo Aplic Aut Mais") && string.IsNullOrEmpty(entrada) && string.IsNullOrEmpty(saida))
            return null;
        
        if (descricao.Contains("Res Aplic Aut Mais") && string.IsNullOrEmpty(entrada) && string.IsNullOrEmpty(saida))
            return null;
        
        return new ItauTransaction
        {
            Data = data,
            Descricao = descricao,
            EntradasRS = entrada,
            SaidasRS = saida,
            SaldoRS = saldo
        };
    }
    
    private async Task<byte[]> CreateExcelAsync(Dictionary<string, List<ItauTransaction>> accountsData)
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
                worksheet.Cell(1, 1).Value = "data";
                worksheet.Cell(1, 2).Value = "descrição";
                worksheet.Cell(1, 3).Value = "entradas R$ (créditos)";
                worksheet.Cell(1, 4).Value = "saídas R$ (débitos)";
                worksheet.Cell(1, 5).Value = "saldo R$";
                
                // Estilo do header
                var headerRange = worksheet.Range(1, 1, 1, 5);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                
                // Dados
                for (int i = 0; i < transactions.Count; i++)
                {
                    var t = transactions[i];
                    worksheet.Cell(i + 2, 1).Value = t.Data;
                    worksheet.Cell(i + 2, 2).Value = t.Descricao;
                    worksheet.Cell(i + 2, 3).Value = t.EntradasRS;
                    worksheet.Cell(i + 2, 4).Value = t.SaidasRS;
                    worksheet.Cell(i + 2, 5).Value = t.SaldoRS;
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