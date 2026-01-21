using PdfProcessor.Core.Models;

namespace PdfProcessor.Core.Interfaces;

public interface IMorganStanleyParser
{
  Task<List<MorganStanleyTransaction>> ParsePdfAsync(Stream pdfStream, string fileName);
}