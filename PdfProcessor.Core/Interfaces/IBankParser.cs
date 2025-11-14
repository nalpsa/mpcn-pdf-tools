namespace PdfProcessor.Core.Interfaces;

using PdfProcessor.Core.Entities;

/// <summary>
/// Interface base para parsers de bancos específicos
/// </summary>
/// <typeparam name="T">Tipo de dado extraído (geralmente BankAccount ou Transaction)</typeparam>
public interface IBankParser<T>
{
  /// <summary>
  /// Nome do banco
  /// </summary>
  string BankName { get; }

  /// <summary>
  /// Formatos/seções suportados pelo parser
  /// </summary>
  string[] SupportedFormats { get; }

  /// <summary>
  /// Processa um único PDF
  /// </summary>
  /// <param name="pdfStream">Stream do arquivo PDF</param>
  /// <param name="fileName">Nome do arquivo</param>
  /// <returns>Resultado do processamento</returns>
  Task<ProcessingResult<List<T>>> ParseAsync(Stream pdfStream, string fileName);

  /// <summary>
  /// Processa múltiplos PDFs em lote
  /// </summary>
  /// <param name="files">Dicionário com nome do arquivo e stream</param>
  /// <returns>Resultado consolidado do processamento</returns>
  Task<ProcessingResult<Dictionary<string, List<T>>>> ParseBatchAsync(
      Dictionary<string, Stream> files);

  /// <summary>
  /// Valida se o PDF pode ser processado por este parser
  /// </summary>
  /// <param name="pdfStream">Stream do arquivo PDF</param>
  /// <returns>True se o parser pode processar o PDF</returns>
  Task<bool> CanParseAsync(Stream pdfStream);
}