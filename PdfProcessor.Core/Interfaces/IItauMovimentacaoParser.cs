using PdfProcessor.Core.Models;

namespace PdfProcessor.Core.Interfaces;

public interface IItauMovimentacaoParser
{
    /// <summary>
    /// Processa múltiplos PDFs de movimentação do Itaú
    /// </summary>
    /// <param name="pdfStreams">Lista de streams dos PDFs</param>
    /// <returns>Bytes do Excel consolidado com múltiplas abas</returns>
    Task<byte[]> ProcessBatchAsync(List<Stream> pdfStreams);
    
    /// <summary>
    /// Extrai movimentações de um único PDF
    /// </summary>
    /// <param name="pdfStream">Stream do PDF</param>
    /// <returns>Dicionário com chave = "AG XXXX - CC XXXXX" e valor = lista de transações</returns>
    Task<Dictionary<string, List<ItauTransaction>>> ExtractMovimentacaoAsync(Stream pdfStream);
}