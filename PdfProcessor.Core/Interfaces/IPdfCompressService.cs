namespace PdfProcessor.Core.Interfaces;

using PdfProcessor.Core.Enums;

/// <summary>
/// Serviço para compressão de arquivos PDF
/// </summary>
public interface IPdfCompressService
{
  /// <summary>
  /// Comprime um único PDF
  /// </summary>
  /// <param name="pdfStream">Stream do PDF</param>
  /// <param name="compressionLevel">Nível de compressão</param>
  /// <param name="removeImages">Se true, remove todas as imagens</param>
  /// <param name="fileName">Nome do arquivo original</param>
  /// <returns>Resultado da compressão</returns>
  Task<CompressionResult> CompressPdfAsync(
      Stream pdfStream,
      CompressionLevel compressionLevel,
      bool removeImages = false,
      string fileName = "");

  /// <summary>
  /// Comprime múltiplos PDFs
  /// </summary>
  /// <param name="files">Dicionário com nome e stream dos arquivos</param>
  /// <param name="compressionSettings">Configurações de compressão por arquivo</param>
  /// <returns>Dicionário com resultados da compressão</returns>
  Task<Dictionary<string, CompressionResult>> CompressBatchAsync(
      Dictionary<string, Stream> files,
      Dictionary<string, CompressionSettings> compressionSettings);
}

/// <summary>
/// Resultado de uma operação de compressão
/// </summary>
public class CompressionResult
{
  public bool Success { get; set; }
  public byte[]? CompressedData { get; set; }
  public long OriginalSizeBytes { get; set; }
  public long CompressedSizeBytes { get; set; }
  public double CompressionRatio => OriginalSizeBytes > 0
      ? (double)CompressedSizeBytes / OriginalSizeBytes
      : 0;
  public string CompressionPercentage => $"{(1 - CompressionRatio) * 100:F2}%";
  public string OriginalSizeMB => $"{OriginalSizeBytes / 1024.0 / 1024.0:F2} MB";
  public string CompressedSizeMB => $"{CompressedSizeBytes / 1024.0 / 1024.0:F2} MB";
  public string? ErrorMessage { get; set; }
}

/// <summary>
/// Configurações de compressão para um arquivo
/// </summary>
public class CompressionSettings
{
  public CompressionLevel Level { get; set; } = CompressionLevel.Medium;
  public bool RemoveImages { get; set; } = false;
  public int? ImageQuality { get; set; } = 85; // 1-100
  public int? MaxImageDpi { get; set; } = 150;
}