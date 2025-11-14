namespace PdfProcessor.Core.Entities;

/// <summary>
/// Resultado genérico de processamento com suporte a dados, erros e avisos
/// </summary>
/// <typeparam name="T">Tipo de dados retornado</typeparam>
public class ProcessingResult<T>
{
    /// <summary>
    /// Indica se o processamento foi bem-sucedido
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Dados resultantes do processamento
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Lista de erros encontrados
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Lista de avisos (não impedem o processamento)
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Quantidade de arquivos processados
    /// </summary>
    public int ProcessedFiles { get; set; }

    /// <summary>
    /// Quantidade de arquivos com erro
    /// </summary>
    public int FailedFiles { get; set; }

    /// <summary>
    /// Tempo de processamento
    /// </summary>
    public TimeSpan ProcessingTime { get; set; }

    /// <summary>
    /// Mensagem adicional de contexto
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Indica se há erros
    /// </summary>
    public bool HasErrors => Errors.Any();

    /// <summary>
    /// Indica se há avisos
    /// </summary>
    public bool HasWarnings => Warnings.Any();

    /// <summary>
    /// Cria um resultado de sucesso
    /// </summary>
    public static ProcessingResult<T> SuccessResult(T data, string? message = null)
    {
        return new ProcessingResult<T>
        {
            Success = true,
            Data = data,
            Message = message
        };
    }

    /// <summary>
    /// Cria um resultado de erro
    /// </summary>
    public static ProcessingResult<T> ErrorResult(string error, string? message = null)
    {
        return new ProcessingResult<T>
        {
            Success = false,
            Errors = new List<string> { error },
            Message = message
        };
    }

    /// <summary>
    /// Cria um resultado de erro com múltiplos erros
    /// </summary>
    public static ProcessingResult<T> ErrorResult(List<string> errors, string? message = null)
    {
        return new ProcessingResult<T>
        {
            Success = false,
            Errors = errors,
            Message = message
        };
    }

    /// <summary>
    /// Adiciona um erro
    /// </summary>
    public void AddError(string error)
    {
        Errors.Add(error);
        Success = false;
    }

    /// <summary>
    /// Adiciona um aviso
    /// </summary>
    public void AddWarning(string warning)
    {
        Warnings.Add(warning);
    }
}