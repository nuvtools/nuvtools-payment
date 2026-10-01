using System.Text.Json.Serialization;

namespace NuvTools.Payment.BancoDoBrasil.ApiClient.DTOs.Requests;

/// <summary>
/// Item (lançamento) de um lote de pagamento de guias com código de barras.
/// </summary>
public class GuiaBatchPaymentItem
{
    /// <summary>Código de barras numérico da guia de recolhimento.</summary>
    [JsonPropertyName("codigoBarras")]
    public string? CodigoBarras { get; set; }

    /// <summary>Data da efetuação do pagamento, no formato inteiro <c>ddMMyyyy</c> (ex.: 30092026).</summary>
    [JsonPropertyName("dataPagamento")]
    public long DataPagamento { get; set; }

    [JsonPropertyName("valorPagamento")]
    public decimal ValorPagamento { get; set; }

    /// <summary>Campo de uso livre do cliente (sem tratamento pelo Banco).</summary>
    [JsonPropertyName("descricaoPagamento")]
    public string? DescricaoPagamento { get; set; }

    /// <summary>Nº de uso livre do cliente (equivalente ao "seu número").</summary>
    [JsonPropertyName("codigoSeuDocumento")]
    public string? CodigoSeuDocumento { get; set; }
}
