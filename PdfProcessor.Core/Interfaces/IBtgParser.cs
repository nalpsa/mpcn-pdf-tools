using PdfProcessor.Core.Models;

namespace PdfProcessor.Core.Interfaces;

public interface IBtgParser
{
    Task<List<BtgTransaction>> ParsePdfAsync(Stream pdfStream, string fileName);
}