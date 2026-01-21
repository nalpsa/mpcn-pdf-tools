using PdfProcessor.Core.Models;

namespace PdfProcessor.Core.Interfaces;

public interface IUbsParser
{
  Task<List<UbsTransaction>> ParsePdfAsync(Stream pdfStream, string fileName);
}