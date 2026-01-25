namespace PdfProcessor.Core.Models;

public class JuliusBarTransaction
{
    public string TradeDate { get; set; } = "";
    public string Type { get; set; } = ""; // Inclui Currency na segunda linha
    public string Quantity { get; set; } = "";
    public string Details { get; set; } = ""; // Inclui ISIN na segunda linha
    public string Amount { get; set; } = "";
    public string ExchangeRate { get; set; } = "";
    public string ReportingCurrency { get; set; } = ""; // Sempre USD
}
