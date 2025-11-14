namespace PdfProcessor.Core.Interfaces;

/// <summary>
/// Serviço para mesclar/unir arquivos PDF
/// </summary>
public interface IPdfMergeService
{
  /// <summary>
  /// Mescla múltiplos PDFs em um único arquivo
  /// </summary>
  /// <param name="files">Dicionário ordenado com nome e stream dos arquivos</param>
  /// <param name="pageRanges">Ranges de páginas para cada arquivo (opcional)</param>
  /// <returns>Array de bytes do PDF mesclado</returns>
  Task<byte[]> MergePdfsAsync(
      Dictionary<string, Stream> files,
      Dictionary<string, string>? pageRanges = null);

  /// <summary>
  /// Valida um range de páginas (ex: "1-5", "1,3,5", "all")
  /// </summary>
  /// <param name="pageRange">Range de páginas</param>
  /// <param name="totalPages">Total de páginas do PDF</param>
  /// <returns>Lista de números de páginas válidas</returns>
  List<int> ParsePageRange(string pageRange, int totalPages);

  /// <summary>
  /// Extrai páginas específicas de um PDF
  /// </summary>
  /// <param name="pdfStream">Stream do PDF</param>
  /// <param name="pageNumbers">Números das páginas a extrair (1-indexed)</param>
  /// <returns>Array de bytes do PDF com as páginas extraídas</returns>
  Task<byte[]> ExtractPagesAsync(Stream pdfStream, List<int> pageNumbers);
}