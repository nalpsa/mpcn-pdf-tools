using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PdfProcessor.Core.Interfaces;
using ClosedXML.Excel;

namespace PdfProcessor.API.Controllers;

[ApiController]
[Route("api/morgan-stanley")]
public class MorganStanleyController : ControllerBase
{
    private readonly IMorganStanleyParser _parser;
    private readonly ILogger<MorganStanleyController> _logger;

    public MorganStanleyController(IMorganStanleyParser parser, ILogger<MorganStanleyController> logger)
    {
        _parser = parser;
        _logger = logger;
    }

    [HttpPost("batch")]
    public async Task<IActionResult> ProcessBatch([FromForm] List<IFormFile> files)
    {
        try
        {
            if (files == null || files.Count == 0)
            {
                return BadRequest("Nenhum arquivo enviado");
            }

            _logger.LogInformation($"🏦 Processando {files.Count} arquivo(s) Morgan Stanley");

            var allTransactions = new List<(string FileName, Core.Models.MorganStanleyTransaction Transaction)>();

            // Processar cada PDF
            foreach (var file in files)
            {
                if (file.Length == 0) continue;

                _logger.LogInformation($"📄 Processando: {file.FileName}");

                // ✅ Copiar para MemoryStream (padrão)
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                stream.Position = 0;
                
                var transactions = await _parser.ParsePdfAsync(stream, file.FileName);

                foreach (var transaction in transactions)
                {
                    allTransactions.Add((file.FileName, transaction));
                }

                _logger.LogInformation($"✅ {transactions.Count} transação(ões) extraída(s) de {file.FileName}");
            }

            if (allTransactions.Count == 0)
            {
                return BadRequest("Nenhuma transação encontrada nos PDFs");
            }

            _logger.LogInformation($"📊 Total: {allTransactions.Count} transação(ões)");

            // Gerar Excel
            var excelBytes = GenerateExcel(allTransactions);

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileName = $"morgan_stanley_{timestamp}.xlsx";

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError($"❌ Erro ao processar PDFs: {ex.Message}");
            return StatusCode(500, $"Erro ao processar: {ex.Message}");
        }
    }

    private byte[] GenerateExcel(List<(string FileName, Core.Models.MorganStanleyTransaction Transaction)> transactions)
    {
        using var workbook = new XLWorkbook();

        // ✅ AGRUPAR POR ACCOUNT NUMBER (cada conta = uma aba)
        var groupedByAccount = transactions
            .GroupBy(t => t.Transaction.AccountNumber)
            .OrderBy(g => g.Key)
            .ToList();

        foreach (var accountGroup in groupedByAccount)
        {
            var accountNumber = accountGroup.Key;
            
            // Criar nome da aba (usar account number)
            var sheetName = string.IsNullOrWhiteSpace(accountNumber) 
                ? "Morgan Stanley" 
                : accountNumber.Length > 31 
                    ? accountNumber.Substring(0, 31) 
                    : accountNumber;
            
            // Remover caracteres inválidos
            sheetName = sheetName.Replace("/", "-").Replace("\\", "-").Replace(":", "-")
                                 .Replace("*", "").Replace("?", "").Replace("[", "").Replace("]", "");
            
            var worksheet = workbook.Worksheets.Add(sheetName);

            // ✅ CABEÇALHOS (8 colunas - sem Account Number)
            var headers = new[]
            {
                "Activity Date",
                "Settlement Date",
                "Activity Type",
                "Description",
                "Comments",
                "Quantity",
                "Price",
                "Credits/(Debits)"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#003D7A"); // Azul Morgan Stanley
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            // ✅ DADOS (8 colunas)
            int row = 2;
            foreach (var (fileName, transaction) in accountGroup)
            {
                worksheet.Cell(row, 1).Value = transaction.ActivityDate;
                worksheet.Cell(row, 2).Value = transaction.SettlementDate;
                worksheet.Cell(row, 3).Value = transaction.ActivityType;
                worksheet.Cell(row, 4).Value = transaction.Description;
                worksheet.Cell(row, 5).Value = transaction.Comments;
                worksheet.Cell(row, 6).Value = transaction.Quantity;
                worksheet.Cell(row, 7).Value = transaction.Price;
                worksheet.Cell(row, 8).Value = transaction.CreditsDebits;
                row++;
            }

            // Auto-ajustar colunas
            worksheet.Columns().AdjustToContents();

            // Congelar primeira linha
            worksheet.SheetView.FreezeRows(1);
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
