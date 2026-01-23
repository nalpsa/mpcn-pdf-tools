namespace PdfProcessor.Core.Interfaces;

/// <summary>
/// Serviço para edição de PDFs (rotação e remoção de páginas)
/// </summary>
public interface IPdfEditService
{
  /// <summary>
  /// Gera thumbnail de uma página específica do PDF
  /// </summary>
  /// <param name="pdfStream">Stream do PDF</param>
  /// <param name="pageNumber">Número da página (1-based)</param>
  /// <param name="maxWidth">Largura máxima da thumbnail</param>
  /// <returns>Thumbnail em base64</returns>
  Task<string> GeneratePageThumbnailAsync(Stream pdfStream, int pageNumber, int maxWidth = 200);

  /// <summary>
  /// Obtém o número total de páginas do PDF
  /// </summary>
  /// <param name="pdfStream">Stream do PDF</param>
  /// <returns>Número de páginas</returns>
  Task<int> GetPageCountAsync(Stream pdfStream);

  /// <summary>
  /// Processa PDF aplicando rotações e removendo páginas
  /// </summary>
  /// <param name="pdfStream">Stream do PDF original</param>
  /// <param name="pageOperations">Lista de operações por página</param>
  /// <param name="originalFileName">Nome do arquivo original</param>
  /// <returns>Resultado do processamento</returns>
  Task<PdfEditResult> ProcessPdfAsync(
      Stream pdfStream,
      List<PageOperation> pageOperations,
      string originalFileName = "");
}

/// <summary>
/// Operação a ser aplicada em uma página específica
/// </summary>
public class PageOperation
{
  /// <summary>
  /// Número da página (1-based)
  /// </summary>
  public int PageNumber { get; set; }

  /// <summary>
  /// Rotação a ser aplicada (0, 90, 180, 270)
  /// </summary>
  public int Rotation { get; set; }

  /// <summary>
  /// Se true, mantém a página; se false, remove
  /// </summary>
  public bool Keep { get; set; }
}

/// <summary>
/// Resultado do processamento de edição
/// </summary>
public class PdfEditResult
{
  public bool Success { get; set; }
  public byte[]? ProcessedPdfData { get; set; }
  public int OriginalPageCount { get; set; }
  public int FinalPageCount { get; set; }
  public int PagesRotated { get; set; }
  public int PagesRemoved { get; set; }
  public string? ErrorMessage { get; set; }
}