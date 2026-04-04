using System.Text.Json.Serialization;

namespace NuvTools.Payment.BancoDoBrasil.ApiClient.DTOs.Responses;

/// <summary>
/// Resposta da criacao de lote de pagamentos no BB.
/// </summary>
public class CreateBatchPaymentResponse
{
    [JsonPropertyName("estadoRequisicao")]
    public int EstadoRequisicao { get; set; }

    [JsonPropertyName("numeroRequisicao")]
    public long NumeroRequisicao { get; set; }

    [JsonPropertyName("quantidadeLancamentos")]
    public int QuantidadeLancamentos { get; set; }

    [JsonPropertyName("valorLancamentos")]
    public decimal ValorLancamentos { get; set; }

    [JsonPropertyName("lancamentos")]
    public List<BatchPaymentItemResponse>? Lancamentos { get; set; }
}
