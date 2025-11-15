namespace PdfProcessor.Core.Models;

/// <summary>
/// Representa uma transação de cash do extrato Itaú
/// </summary>
public class ItauCashTransaction
{
    /// <summary>
    /// Data da operação (formato: DD/MM/YYYY)
    /// </summary>
    public string OperationDate { get; set; } = string.Empty;
    
    /// <summary>
    /// Data do valor (formato: DD/MM/YYYY)
    /// </summary>
    public string ValueDate { get; set; } = string.Empty;
    
    /// <summary>
    /// Descrição da transação
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Valor da transação (pode ser negativo)
    /// </summary>
    public string Value { get; set; } = string.Empty;
    
    /// <summary>
    /// Saldo da conta após a transação
    /// </summary>
    public string AccountBalance { get; set; } = string.Empty;
}