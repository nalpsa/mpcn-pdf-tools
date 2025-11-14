namespace PdfProcessor.Core.Exceptions;

/// <summary>
/// Exceção base para erros de processamento de PDF
/// </summary>
public class PdfProcessingException : Exception
{
  public string? FileName { get; set; }
  public string? Details { get; set; }

  public PdfProcessingException(string message) : base(message)
  {
  }

  public PdfProcessingException(string message, Exception innerException)
      : base(message, innerException)
  {
  }

  public PdfProcessingException(string message, string fileName) : base(message)
  {
    FileName = fileName;
  }

  public PdfProcessingException(string message, string fileName, string details)
      : base(message)
  {
    FileName = fileName;
    Details = details;
  }
}

/// <summary>
/// Exceção para erros no parsing de dados bancários
/// </summary>
public class ParserException : PdfProcessingException
{
  public string? BankName { get; set; }
  public string? Section { get; set; }

  public ParserException(string message) : base(message)
  {
  }

  public ParserException(string message, string bankName, string fileName)
      : base(message, fileName)
  {
    BankName = bankName;
  }

  public ParserException(string message, Exception innerException)
      : base(message, innerException)
  {
  }
}

/// <summary>
/// Exceção para PDFs inválidos ou corrompidos
/// </summary>
public class InvalidPdfException : PdfProcessingException
{
  public InvalidPdfException(string message) : base(message)
  {
  }

  public InvalidPdfException(string message, string fileName)
      : base(message, fileName)
  {
  }

  public InvalidPdfException(string message, Exception innerException)
      : base(message, innerException)
  {
  }
}

/// <summary>
/// Exceção para arquivos muito grandes
/// </summary>
public class FileTooLargeException : PdfProcessingException
{
  public long FileSize { get; set; }
  public long MaxSize { get; set; }

  public FileTooLargeException(string fileName, long fileSize, long maxSize)
      : base($"File '{fileName}' is too large. Size: {fileSize / 1024 / 1024}MB, Max: {maxSize / 1024 / 1024}MB", fileName)
  {
    FileSize = fileSize;
    MaxSize = maxSize;
  }
}

/// <summary>
/// Exceção para formato de arquivo não suportado
/// </summary>
public class UnsupportedFormatException : PdfProcessingException
{
  public string? ProvidedFormat { get; set; }
  public string[]? SupportedFormats { get; set; }

  public UnsupportedFormatException(string message) : base(message)
  {
  }

  public UnsupportedFormatException(string fileName, string providedFormat, string[] supportedFormats)
      : base($"File '{fileName}' has unsupported format '{providedFormat}'. Supported: {string.Join(", ", supportedFormats)}", fileName)
  {
    ProvidedFormat = providedFormat;
    SupportedFormats = supportedFormats;
  }
}