namespace PdfProcessor.Core.Enums;

/// <summary>
/// Tipos de bancos suportados pelo sistema
/// </summary>
public enum BankType
{
  /// <summary>
  /// Banco Itaú - Cash Transactions
  /// </summary>
  ItauCashTransactions = 1,

  /// <summary>
  /// Banco Itaú - Movimentação Bancária
  /// </summary>
  ItauMovimentacao = 2,

  /// <summary>
  /// Morgan Stanley
  /// </summary>
  MorganStanley = 3,

  /// <summary>
  /// Julius Baer
  /// </summary>
  JuliusBaer = 4,

  /// <summary>
  /// Julius Bar
  /// </summary>
  JuliusBar = 5,

  /// <summary>
  /// Raymond James
  /// </summary>
  RaymondJames = 6
}