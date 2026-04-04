using System.Text.Json.Serialization;

namespace NuvTools.Payment.BancoDoBrasil.ApiClient.DTOs.Responses;

/// <summary>
/// Item de lancamento na resposta de pagamento do BB.
/// </summary>
public class BatchPaymentItemResponse
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

    [JsonPropertyName("erros")]
    public string? Erros { get; set; }
}
