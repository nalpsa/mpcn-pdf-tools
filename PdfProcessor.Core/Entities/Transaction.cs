namespace PdfProcessor.Core.Entities;

/// <summary>
/// Representa uma transação bancária extraída de PDF
/// </summary>
public class Transaction
{
    /// <summary>
    /// Data da transação
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Descrição/histórico da transação
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Valor de débito (saída)
    /// </summary>
    public decimal? Debit { get; set; }

    /// <summary>
    /// Valor de crédito (entrada)
    /// </summary>
    public decimal? Credit { get; set; }

    /// <summary>
    /// Saldo após a transação
    /// </summary>
    public decimal Balance { get; set; }

    /// <summary>
    /// Moeda da transação (USD, BRL, EUR, etc.)
    /// </summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Número da conta associada
    /// </summary>
    public string AccountNumber { get; set; } = string.Empty;

    /// <summary>
    /// Nome do arquivo de origem
    /// </summary>
    public string SourceFileName { get; set; } = string.Empty;

    /// <summary>
    /// Obtém o valor da transação (débito ou crédito)
    /// </summary>
    public decimal Amount => Credit ?? Debit ?? 0;

    /// <summary>
    /// Indica se é uma transação de crédito (entrada)
    /// </summary>
    public bool IsCredit => Credit.HasValue && Credit.Value > 0;

    /// <summary>
    /// Indica se é uma transação de débito (saída)
    /// </summary>
    public bool IsDebit => Debit.HasValue && Debit.Value > 0;
}