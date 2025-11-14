namespace PdfProcessor.Shared.Constants;

/// <summary>
/// Constantes relacionadas a arquivos
/// </summary>
public static class FileConstants
{
  /// <summary>
  /// Tamanho máximo de arquivo em bytes (16MB)
  /// </summary>
  public const long MaxFileSizeBytes = 16 * 1024 * 1024;

  /// <summary>
  /// Extensões de arquivo permitidas
  /// </summary>
  public static readonly string[] AllowedExtensions = { ".pdf" };

  /// <summary>
  /// Content-Type para PDF
  /// </summary>
  public const string PdfContentType = "application/pdf";

  /// <summary>
  /// Content-Type para Excel
  /// </summary>
  public const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

  /// <summary>
  /// Content-Type para ZIP
  /// </summary>
  public const string ZipContentType = "application/zip";

  /// <summary>
  /// Pasta de upload temporário
  /// </summary>
  public const string UploadFolder = "uploads";

  /// <summary>
  /// Pasta de saída
  /// </summary>
  public const string OutputFolder = "outputs";

  /// <summary>
  /// Pasta temporária
  /// </summary>
  public const string TempFolder = "temp";
}

/// <summary>
/// Mensagens de erro padronizadas
/// </summary>
public static class ErrorMessages
{
  public const string NoFilesSelected = "Nenhum arquivo foi selecionado";
  public const string InvalidFileType = "Tipo de arquivo não permitido. Apenas PDFs são aceitos";
  public const string FileTooLarge = "Arquivo muito grande. Tamanho máximo: 16MB";
  public const string CorruptedPdf = "O arquivo PDF está corrompido ou inválido";
  public const string NoDataExtracted = "Nenhum dado foi extraído do PDF";
  public const string ProcessingError = "Erro ao processar o arquivo";
  public const string InsufficientFiles = "Número insuficiente de arquivos para realizar esta operação";
  public const string InvalidPageRange = "Range de páginas inválido";
  public const string InvalidRotationAngle = "Ângulo de rotação inválido. Use: 0, 90, 180 ou 270";
  public const string MergeError = "Erro ao mesclar arquivos PDF";
  public const string CompressionError = "Erro ao comprimir arquivo PDF";
  public const string ExcelGenerationError = "Erro ao gerar arquivo Excel";
}

/// <summary>
/// Mensagens de sucesso padronizadas
/// </summary>
public static class SuccessMessages
{
  public const string ProcessingComplete = "Processamento concluído com sucesso";
  public const string FilesProcessed = "Arquivos processados com sucesso";
  public const string ExcelGenerated = "Arquivo Excel gerado com sucesso";
  public const string PdfsMerged = "PDFs mesclados com sucesso";
  public const string PdfsRotated = "PDFs rotacionados com sucesso";
  public const string PdfsCompressed = "PDFs comprimidos com sucesso";
}

/// <summary>
/// Configurações padrão
/// </summary>
public static class DefaultSettings
{
  public const int DefaultThumbnailWidth = 200;
  public const int DefaultThumbnailHeight = 200;
  public const int DefaultImageQuality = 85;
  public const int DefaultMaxImageDpi = 150;
  public const int MaxBatchSize = 50;
  public const int ProcessingTimeoutSeconds = 300; // 5 minutos
}