using System;
using System.Collections.Generic;

namespace PdfProcessor.Application.DTOs;

/// <summary>
/// DTO base para requisições de upload
/// Application Layer não deve depender de IFormFile (infraestrutura web)
/// </summary>
public class UploadRequestDto
{
  public List<FileUploadDto> Files { get; set; } = new();
  public string BankType { get; set; } = string.Empty;
}

/// <summary>
/// DTO para representar arquivo uploaded (independente de framework web)
/// </summary>
public class FileUploadDto
{
  public string FileName { get; set; } = string.Empty;
  public string ContentType { get; set; } = string.Empty;
  public long Length { get; set; }
  public Stream Content { get; set; } = Stream.Null;
}

/// <summary>
/// DTO para rotação de PDFs
/// </summary>
public class RotatePdfRequestDto
{
  public List<FileUploadDto> Files { get; set; } = new();
  public List<int> RotationAngles { get; set; } = new(); // 90, 180, 270
  public List<int> FileIndices { get; set; } = new();
}

/// <summary>
/// DTO para mesclagem de PDFs
/// </summary>
public class MergePdfRequestDto
{
  public List<FileUploadDto> Files { get; set; } = new();
  public List<int> Order { get; set; } = new();
  public List<string> PageRanges { get; set; } = new(); // "1-5", "all", "1,3,5"
}

/// <summary>
/// DTO para compressão de PDFs
/// </summary>
public class CompressPdfRequestDto
{
  public List<FileUploadDto> Files { get; set; } = new();
  public List<string> CompressionLevels { get; set; } = new(); // "low", "medium", "high"
  public List<bool> RemoveImages { get; set; } = new();
}

/// <summary>
/// DTO para extração Itaú Cash Transactions
/// </summary>
public class ItauCashTransactionsRequestDto
{
  public List<FileUploadDto> Files { get; set; } = new();
}

/// <summary>
/// DTO para extração Itaú Movimentação
/// </summary>
public class ItauMovimentacaoRequestDto
{
  public List<FileUploadDto> Files { get; set; } = new();
}