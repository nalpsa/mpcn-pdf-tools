namespace PdfProcessor.Core.Models;

public class BtgTransaction
{
    public string ProcessSettlementDate { get; set; } = string.Empty;
    public string TradeTransactionDate { get; set; } = string.Empty;
    public string ActivityType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Quantity { get; set; } = string.Empty;
    public string Price { get; set; } = string.Empty;
    public string AccruedInterest { get; set; } = string.Empty;
    public string Amount { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
}