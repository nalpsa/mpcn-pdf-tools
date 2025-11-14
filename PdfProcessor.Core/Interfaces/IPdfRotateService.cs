namespace PdfProcessor.Core.Interfaces;

using PdfProcessor.Core.Enums;

/// <summary>
/// Serviço para rotação de arquivos PDF
/// </summary>
public interface IPdfRotateService
{
  /// <summary>
  /// Rotaciona um único PDF
  /// </summary>
  /// <param name="pdfStream">Stream do PDF</param>
  /// <param name="rotationAngle">Ângulo de rotação</param>
  /// <param name="fileName">Nome do arquivo original</param>
  /// <returns>Array de bytes do PDF rotacionado</returns>
  Task<byte[]> RotatePdfAsync(Stream pdfStream, RotationAngle rotationAngle, string fileName);

  /// <summary>
  /// Rotaciona múltiplos PDFs com diferentes ângulos
  /// </summary>
  /// <param name="files">Dicionário com nome e stream dos arquivos</param>
  /// <param name="rotations">Dicionário com nome e ângulo de rotação</param>
  /// <returns>Dicionário com nome e bytes dos PDFs rotacionados</returns>
  Task<Dictionary<string, byte[]>> RotateBatchAsync(
      Dictionary<string, Stream> files,
      Dictionary<string, RotationAngle> rotations);

  /// <summary>
  /// Gera miniatura (thumbnail) da primeira página do PDF
  /// </summary>
  /// <param name="pdfStream">Stream do PDF</param>
  /// <param name="width">Largura da miniatura em pixels</param>
  /// <param name="height">Altura da miniatura em pixels</param>
  /// <returns>Array de bytes da imagem PNG</returns>
  Task<byte[]> GenerateThumbnailAsync(Stream pdfStream, int width = 200, int height = 200);

  /// <summary>
  /// Obtém informações do PDF (número de páginas, tamanho, etc.)
  /// </summary>
  /// <param name="pdfStream">Stream do PDF</param>
  /// <returns>Informações do PDF</returns>
  Task<PdfInfo> GetPdfInfoAsync(Stream pdfStream);
}

/// <summary>
/// Informações de um arquivo PDF
/// </summary>
public class PdfInfo
{
  public int PageCount { get; set; }
  public long FileSizeBytes { get; set; }
  public string FileSizeMB => $"{FileSizeBytes / 1024.0 / 1024.0:F2} MB";
  public DateTime? CreationDate { get; set; }
  public DateTime? ModificationDate { get; set; }
  public string? Author { get; set; }
  public string? Title { get; set; }
  public string? Subject { get; set; }
  public string? FileName { get; set; }
}