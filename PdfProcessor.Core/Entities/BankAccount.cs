namespace PdfProcessor.Core.Entities;

/// <summary>
/// Representa uma conta bancária com suas transações
/// </summary>
public class BankAccount
{
    /// <summary>
    /// Número da conta
    /// </summary>
    public string AccountNumber { get; set; } = string.Empty;

    /// <summary>
    /// Moeda da conta
    /// </summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Agência (quando aplicável)
    /// </summary>
    public string? Branch { get; set; }

    /// <summary>
    /// Lista de transações da conta
    /// </summary>
    public List<Transaction> Transactions { get; set; } = new();

    /// <summary>
    /// Saldo inicial da conta (se disponível)
    /// </summary>
    public decimal? OpeningBalance { get; set; }

    /// <summary>
    /// Saldo final da conta (se disponível)
    /// </summary>
    public decimal? ClosingBalance { get; set; }

    /// <summary>
    /// Período inicial das transações
    /// </summary>
    public DateTime? PeriodStart { get; set; }

    /// <summary>
    /// Período final das transações
    /// </summary>
    public DateTime? PeriodEnd { get; set; }

    /// <summary>
    /// Nome completo da conta (ex: "ag 7816 cc 01990-0 - BRL")
    /// </summary>
    public string DisplayName => $"{AccountNumber} - {Currency}";

    /// <summary>
    /// Total de transações
    /// </summary>
    public int TransactionCount => Transactions.Count;

    /// <summary>
    /// Total de créditos
    /// </summary>
    public decimal TotalCredits => Transactions.Where(t => t.IsCredit).Sum(t => t.Credit ?? 0);

    /// <summary>
    /// Total de débitos
    /// </summary>
    public decimal TotalDebits => Transactions.Where(t => t.IsDebit).Sum(t => t.Debit ?? 0);
}