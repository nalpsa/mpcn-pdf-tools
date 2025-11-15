namespace PdfProcessor.Core.Models;

/// <summary>
/// Representa uma transação bancária do extrato Itaú
/// </summary>
public class ItauTransaction
{
    /// <summary>
    /// Data da transação (formato: dd/MM)
    /// </summary>
    public string Data { get; set; } = string.Empty;
    
    /// <summary>
    /// Descrição da movimentação
    /// </summary>
    public string Descricao { get; set; } = string.Empty;
    
    /// <summary>
    /// Valor de entrada/crédito (formato: "1.234,56")
    /// </summary>
    public string EntradasRS { get; set; } = string.Empty;
    
    /// <summary>
    /// Valor de saída/débito (formato: "1.234,56-")
    /// </summary>
    public string SaidasRS { get; set; } = string.Empty;
    
    /// <summary>
    /// Saldo após a transação (formato: "1.234,56")
    /// </summary>
    public string SaldoRS { get; set; } = string.Empty;
}