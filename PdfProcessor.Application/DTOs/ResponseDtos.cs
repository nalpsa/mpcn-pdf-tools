namespace PdfProcessor.Application.DTOs;

/// <summary>
/// DTO base para respostas
/// </summary>
public abstract class BaseResponseDto
{
  public bool Success { get; set; }
  public string? Message { get; set; }
  public List<string> Errors { get; set; } = new();
  public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// DTO de resposta para processamento de arquivos
/// </summary>
public class FileProcessingResponseDto : BaseResponseDto
{
  public int ProcessedFiles { get; set; }
  public int FailedFiles { get; set; }
  public byte[]? FileData { get; set; }
  public string? FileName { get; set; }
  public string? ContentType { get; set; }
  public long? FileSizeBytes { get; set; }
  public TimeSpan ProcessingTime { get; set; }
}

/// <summary>
/// DTO de resposta para extração bancária
/// </summary>
public class BankExtractionResponseDto : BaseResponseDto
{
  public byte[] ExcelData { get; set; } = Array.Empty<byte>();
  public string FileName { get; set; } = string.Empty;
  public int TotalAccounts { get; set; }
  public int TotalTransactions { get; set; }
  public Dictionary<string, int> TransactionsByAccount { get; set; } = new();
  public TimeSpan ProcessingTime { get; set; }
}

/// <summary>
/// DTO de resposta para informações de PDF
/// </summary>
public class PdfInfoResponseDto : BaseResponseDto
{
  public int PageCount { get; set; }
  public string FileSize { get; set; } = string.Empty;
  public string? Author { get; set; }
  public string? Title { get; set; }
  public DateTime? CreationDate { get; set; }
  public DateTime? ModificationDate { get; set; }
  public byte[]? ThumbnailData { get; set; }
}

/// <summary>
/// DTO de resposta para rotação de PDFs
/// </summary>
public class RotatePdfResponseDto : BaseResponseDto
{
  public byte[] ZipData { get; set; } = Array.Empty<byte>();
  public string ZipFileName { get; set; } = string.Empty;
  public int RotatedFiles { get; set; }
  public Dictionary<string, string> RotationDetails { get; set; } = new();
}

/// <summary>
/// DTO de resposta para mesclagem de PDFs
/// </summary>
public class MergePdfResponseDto : BaseResponseDto
{
  public byte[] PdfData { get; set; } = Array.Empty<byte>();
  public string FileName { get; set; } = string.Empty;
  public int MergedFiles { get; set; }
  public int TotalPages { get; set; }
  public long FileSizeBytes { get; set; }
}

/// <summary>
/// DTO de resposta para compressão de PDFs
/// </summary>
public class CompressPdfResponseDto : BaseResponseDto
{
  public byte[] ZipData { get; set; } = Array.Empty<byte>();
  public string ZipFileName { get; set; } = string.Empty;
  public int CompressedFiles { get; set; }
  public long OriginalTotalSize { get; set; }
  public long CompressedTotalSize { get; set; }
  public double AverageCompressionRatio { get; set; }
  public Dictionary<string, CompressionDetailDto> CompressionDetails { get; set; } = new();
}

/// <summary>
/// Detalhes de compressão de um arquivo
/// </summary>
public class CompressionDetailDto
{
  public long OriginalSize { get; set; }
  public long CompressedSize { get; set; }
  public string CompressionPercentage { get; set; } = string.Empty;
  public bool Success { get; set; }
  public string? ErrorMessage { get; set; }
}