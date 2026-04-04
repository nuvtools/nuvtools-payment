using System.Text.Json.Serialization;

namespace NuvTools.Payment.BancoDoBrasil.ApiClient.DTOs.Requests;

/// <summary>
/// Item de lancamento no lote de pagamentos de boleto do BB.
/// </summary>
public class BatchPaymentItemRequest
{
    [JsonPropertyName("numeroSequencialLancamento")]
    public int NumeroSequencialLancamento { get; set; }

    [JsonPropertyName("codigoBarras")]
    public string? CodigoBarras { get; set; }

    [JsonPropertyName("dataVencimento")]
    public string? DataVencimento { get; set; }

    [JsonPropertyName("dataPagamento")]
    public string? DataPagamento { get; set; }

    [JsonPropertyName("valorPagamento")]
    public decimal ValorPagamento { get; set; }

    [JsonPropertyName("valorNominal")]
    public decimal ValorNominal { get; set; }

    [JsonPropertyName("descricaoPagamento")]
    public string? DescricaoPagamento { get; set; }
}
