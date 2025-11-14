namespace PdfProcessor.Core.Enums;

/// <summary>
/// Ângulos de rotação suportados
/// </summary>
public enum RotationAngle
{
  /// <summary>
  /// Sem rotação
  /// </summary>
  None = 0,

  /// <summary>
  /// 90 graus (sentido horário)
  /// </summary>
  Rotate90 = 90,

  /// <summary>
  /// 180 graus
  /// </summary>
  Rotate180 = 180,

  /// <summary>
  /// 270 graus (90 graus anti-horário)
  /// </summary>
  Rotate270 = 270
}