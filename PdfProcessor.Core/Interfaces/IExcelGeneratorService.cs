namespace PdfProcessor.Core.Interfaces;

using PdfProcessor.Core.Entities;

/// <summary>
/// Serviço para geração de arquivos Excel
/// </summary>
public interface IExcelGeneratorService
{
  /// <summary>
  /// Gera um arquivo Excel com múltiplas abas
  /// </summary>
  /// <typeparam name="T">Tipo dos dados a serem exportados</typeparam>
  /// <param name="dataBySheet">Dicionário com nome da aba e lista de dados</param>
  /// <param name="sheetOptions">Opções de formatação por aba</param>
  /// <returns>Array de bytes do arquivo Excel</returns>
  Task<byte[]> GenerateExcelAsync<T>(
      Dictionary<string, List<T>> dataBySheet,
      Dictionary<string, ExcelSheetOptions>? sheetOptions = null) where T : class;

  /// <summary>
  /// Gera Excel a partir de contas bancárias
  /// </summary>
  /// <param name="accounts">Dicionário com identificador da conta e dados</param>
  /// <param name="bankName">Nome do banco (para título)</param>
  /// <returns>Array de bytes do arquivo Excel</returns>
  Task<byte[]> GenerateBankAccountsExcelAsync(
      Dictionary<string, BankAccount> accounts,
      string bankName = "Bank Statement");

  /// <summary>
  /// Gera Excel a partir de transações agrupadas
  /// </summary>
  /// <param name="transactionsByAccount">Transações agrupadas por conta</param>
  /// <param name="bankName">Nome do banco</param>
  /// <returns>Array de bytes do arquivo Excel</returns>
  Task<byte[]> GenerateTransactionsExcelAsync(
      Dictionary<string, List<Transaction>> transactionsByAccount,
      string bankName = "Transactions");
}

/// <summary>
/// Opções de formatação para uma aba do Excel
/// </summary>
public class ExcelSheetOptions
{
  /// <summary>
  /// Título da aba (se diferente do nome)
  /// </summary>
  public string? Title { get; set; }

  /// <summary>
  /// Aplicar auto-filtro nas colunas
  /// </summary>
  public bool AutoFilter { get; set; } = true;

  /// <summary>
  /// Congelar primeira linha (cabeçalho)
  /// </summary>
  public bool FreezeHeaderRow { get; set; } = true;

  /// <summary>
  /// Ajustar largura das colunas automaticamente
  /// </summary>
  public bool AutoFitColumns { get; set; } = true;

  /// <summary>
  /// Aplicar formatação de tabela
  /// </summary>
  public bool FormatAsTable { get; set; } = true;

  /// <summary>
  /// Incluir linha de totais
  /// </summary>
  public bool IncludeTotalsRow { get; set; } = false;

  /// <summary>
  /// Colunas numéricas para formatar como moeda
  /// </summary>
  public List<string> CurrencyColumns { get; set; } = new();

  /// <summary>
  /// Colunas de data para formatar
  /// </summary>
  public List<string> DateColumns { get; set; } = new();
}