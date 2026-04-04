using System.Text.Json.Serialization;

namespace NuvTools.Payment.BancoDoBrasil.ApiClient.DTOs.Responses;

/// <summary>
/// Item da lista de pagamentos na resposta de consulta do BB.
/// </summary>
public class BankSlipPaymentListItemResponse
{
    [JsonPropertyName("numeroSequencialLancamento")]
    public int NumeroSequencialLancamento { get; set; }

    [JsonPropertyName("codigoBarras")]
    public string? CodigoBarras { get; set; }

    [JsonPropertyName("estadoLancamento")]
    public int EstadoLancamento { get; set; }

    [JsonPropertyName("descricaoEstadoLancamento")]
    public string? DescricaoEstadoLancamento { get; set; }

    [JsonPropertyName("valorPagamento")]
    public decimal ValorPagamento { get; set; }

    [JsonPropertyName("dataPagamento")]
    public string? DataPagamento { get; set; }

    [JsonPropertyName("dataVencimento")]
    public string? DataVencimento { get; set; }

    [JsonPropertyName("descricaoPagamento")]
    public string? DescricaoPagamento { get; set; }
}
