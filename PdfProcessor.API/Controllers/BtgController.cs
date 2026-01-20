using Microsoft.AspNetCore.Mvc;
using PdfProcessor.Core.Interfaces;
using ClosedXML.Excel;

namespace PdfProcessor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BtgController : ControllerBase
{
    private readonly IBtgParser _btgParser;
    private readonly ILogger<BtgController> _logger;

    public BtgController(IBtgParser btgParser, ILogger<BtgController> logger)
    {
        _btgParser = btgParser;
        _logger = logger;
    }

    [HttpPost("batch")]
    public async Task<IActionResult> ProcessBatch([FromForm] IFormFileCollection files)
    {
        try
        {
            if (files == null || files.Count == 0)
            {
                return BadRequest("Nenhum arquivo foi enviado");
            }

            _logger.LogInformation($"📦 Recebidos {files.Count} arquivo(s) BTG Pactual");

            var allTransactions = new List<(string FileName, Core.Models.BtgTransaction Transaction)>();

            // Processar cada PDF
            foreach (var file in files)
            {
                if (file.Length == 0) continue;

                _logger.LogInformation($"📄 Processando: {file.FileName}");

                // ✅ CRITICAL: Copiar para MemoryStream como faz o ItauCashController
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                stream.Position = 0;
                
                var transactions = await _btgParser.ParsePdfAsync(stream, file.FileName);

                foreach (var transaction in transactions)
                {
                    allTransactions.Add((file.FileName, transaction));
                }

                _logger.LogInformation($"✅ {transactions.Count} transação(ões) extraída(s) de {file.FileName}");
            }

            if (allTransactions.Count == 0)
            {
                return BadRequest("Nenhuma transação foi encontrada nos PDFs");
            }

            _logger.LogInformation($"📊 Total: {allTransactions.Count} transação(ões)");

            // Gerar Excel
            var excelBytes = GenerateExcel(allTransactions);

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileName = $"btg_pactual_{timestamp}.xlsx";

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError($"❌ Erro ao processar PDFs: {ex.Message}");
            return StatusCode(500, $"Erro ao processar: {ex.Message}");
        }
    }

    private byte[] GenerateExcel(List<(string FileName, Core.Models.BtgTransaction Transaction)> transactions)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("BTG Pactual - Transactions");

        // Cabeçalhos
        var headers = new[]
        {
            "Process/Settlement Date",
            "Trade/Transaction Date",
            "Activity Type",
            "Description",
            "Quantity",
            "Price",
            "Accrued Interest",
            "Amount",
            "Currency"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = worksheet.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        // Dados
        int row = 2;
        foreach (var (fileName, transaction) in transactions)
        {
            worksheet.Cell(row, 1).Value = transaction.ProcessSettlementDate;
            worksheet.Cell(row, 2).Value = transaction.TradeTransactionDate;
            worksheet.Cell(row, 3).Value = transaction.ActivityType;
            worksheet.Cell(row, 4).Value = transaction.Description;
            worksheet.Cell(row, 5).Value = transaction.Quantity;
            worksheet.Cell(row, 6).Value = transaction.Price;
            worksheet.Cell(row, 7).Value = transaction.AccruedInterest;
            worksheet.Cell(row, 8).Value = transaction.Amount;
            worksheet.Cell(row, 9).Value = transaction.Currency;
            row++;
        }

        // Auto-ajustar colunas
        worksheet.Columns().AdjustToContents();

        // Congelar primeira linha
        worksheet.SheetView.FreezeRows(1);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}