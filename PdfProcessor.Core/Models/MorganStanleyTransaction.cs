namespace PdfProcessor.Core.Models;

public class MorganStanleyTransaction
{
  public string AccountNumber { get; set; } = "";
  public string ActivityDate { get; set; } = "";
  public string SettlementDate { get; set; } = "";
  public string ActivityType { get; set; } = "";
  public string Description { get; set; } = "";
  public string Comments { get; set; } = "";
  public string Quantity { get; set; } = "";
  public string Price { get; set; } = "";
  public string CreditsDebits { get; set; } = "";
}