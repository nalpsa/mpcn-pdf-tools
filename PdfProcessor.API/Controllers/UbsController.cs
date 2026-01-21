using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PdfProcessor.Core.Interfaces;
using ClosedXML.Excel;

namespace PdfProcessor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UbsController : ControllerBase
{
  private readonly IUbsParser _parser;
  private readonly ILogger<UbsController> _logger;

  public UbsController(IUbsParser parser, ILogger<UbsController> logger)
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

      _logger.LogInformation($"🏦 Processando {files.Count} arquivo(s) UBS Switzerland");

      var allTransactions = new List<(string FileName, Core.Models.UbsTransaction Transaction)>();

      // Processar cada PDF
      foreach (var file in files)
      {
        if (file.Length == 0) continue;

        _logger.LogInformation($"📄 Processando: {file.FileName}");

        // ✅ Copiar para MemoryStream (padrão do Itaú/BTG)
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
      var fileName = $"ubs_switzerland_{timestamp}.xlsx";

      return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }
    catch (Exception ex)
    {
      _logger.LogError($"❌ Erro ao processar PDFs: {ex.Message}");
      return StatusCode(500, $"Erro ao processar: {ex.Message}");
    }
  }

  private byte[] GenerateExcel(List<(string FileName, Core.Models.UbsTransaction Transaction)> transactions)
  {
    using var workbook = new XLWorkbook();

    // ✅ AGRUPAR POR IBAN (cada IBAN = uma aba)
    var groupedByIban = transactions
        .GroupBy(t => t.Transaction.AccountIban)
        .OrderBy(g => g.Key)
        .ToList();

    foreach (var ibanGroup in groupedByIban)
    {
      var iban = ibanGroup.Key;

      // Criar nome da aba (últimos caracteres do IBAN)
      var sheetName = string.IsNullOrWhiteSpace(iban)
          ? "UBS"
          : iban.Length > 20
              ? iban.Substring(iban.Length - 20)
              : iban;

      // Remover caracteres inválidos
      sheetName = sheetName.Replace("/", "-").Replace("\\", "-").Replace(":", "-");

      var worksheet = workbook.Worksheets.Add(sheetName);

      // ✅ CABEÇALHOS (SEM "Account IBAN")
      var headers = new[]
      {
                "Date",
                "Information",
                "Debits",
                "Credits",
                "Value Date",
                "Balance"
            };

      for (int i = 0; i < headers.Length; i++)
      {
        var cell = worksheet.Cell(1, i + 1);
        cell.Value = headers[i];
        cell.Style.Font.Bold = true;
        cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A8A"); // Azul UBS
        cell.Style.Font.FontColor = XLColor.White;
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
      }

      // ✅ DADOS (SEM coluna Account IBAN)
      int row = 2;
      foreach (var (fileName, transaction) in ibanGroup)
      {
        worksheet.Cell(row, 1).Value = transaction.Date;
        worksheet.Cell(row, 2).Value = transaction.Information;
        worksheet.Cell(row, 3).Value = transaction.Debits;
        worksheet.Cell(row, 4).Value = transaction.Credits;
        worksheet.Cell(row, 5).Value = transaction.ValueDate;
        worksheet.Cell(row, 6).Value = transaction.Balance;
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