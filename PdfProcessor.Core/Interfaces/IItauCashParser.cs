using PdfProcessor.Core.Models;

namespace PdfProcessor.Core.Interfaces;

/// <summary>
/// Interface para parser de Cash Transactions do Itaú
/// </summary>
public interface IItauCashParser
{
    /// <summary>
    /// Processa múltiplos PDFs de Cash Transactions do Itaú
    /// </summary>
    /// <param name="pdfStreams">Lista de streams dos PDFs</param>
    /// <returns>Bytes do Excel consolidado com múltiplas abas (uma por conta/moeda)</returns>
    Task<byte[]> ProcessBatchAsync(List<Stream> pdfStreams);
    
    /// <summary>
    /// Extrai Cash Transactions de um único PDF
    /// </summary>
    /// <param name="pdfStream">Stream do PDF</param>
    /// <returns>Dicionário com chave = "NUMERO_MOEDA" (ex: "6086454001_USD") e valor = lista de transações</returns>
    Task<Dictionary<string, List<ItauCashTransaction>>> ExtractCashTransactionsAsync(Stream pdfStream);
}