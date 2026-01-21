namespace PdfProcessor.Core.Models;

public class UbsTransaction
{
  public string AccountIban { get; set; } = "";
  public string Date { get; set; } = "";
  public string Information { get; set; } = "";
  public string Debits { get; set; } = "";
  public string Credits { get; set; } = "";
  public string ValueDate { get; set; } = "";
  public string Balance { get; set; } = "";
}