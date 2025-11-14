namespace PdfProcessor.Core.Enums;

/// <summary>
/// Níveis de compressão de PDF
/// </summary>
public enum CompressionLevel
{
  /// <summary>
  /// Compressão baixa (maior qualidade)
  /// </summary>
  Low = 1,

  /// <summary>
  /// Compressão média (balanceado)
  /// </summary>
  Medium = 2,

  /// <summary>
  /// Compressão alta (menor tamanho)
  /// </summary>
  High = 3
}