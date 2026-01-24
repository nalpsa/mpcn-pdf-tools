namespace PdfProcessor.Core.Models;

public class ItauCash2Transaction
{
    public string OperationDate { get; set; } = "";
    public string ValueDate { get; set; } = "";
    public string Description { get; set; } = "";
    public string Value { get; set; } = "";
    public string AccountBalance { get; set; } = "";
}