using PdfProcessor.Core.Models;

namespace PdfProcessor.Core.Interfaces;

public interface IItauCash2Parser
{
    Task<byte[]> ProcessBatchAsync(List<Stream> pdfStreams);
    Task<Dictionary<string, List<ItauCash2Transaction>>> ExtractCashTransactionsAsync(Stream pdfStream);
}