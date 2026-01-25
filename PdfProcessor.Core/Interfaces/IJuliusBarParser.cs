using PdfProcessor.Core.Models;

namespace PdfProcessor.Core.Interfaces;

public interface IJuliusBarParser
{
    Task<byte[]> ProcessBatchAsync(List<Stream> pdfStreams);
    Task<Dictionary<string, List<JuliusBarTransaction>>> ExtractTransactionsAsync(Stream pdfStream);
}
