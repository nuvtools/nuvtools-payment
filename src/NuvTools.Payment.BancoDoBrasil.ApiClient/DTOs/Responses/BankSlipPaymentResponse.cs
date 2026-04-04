using System.Text.Json.Serialization;

namespace NuvTools.Payment.BancoDoBrasil.ApiClient.DTOs.Responses;

/// <summary>
/// Resposta da consulta de pagamento de boleto no BB.
/// </summary>
public class BankSlipPaymentResponse
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
    public List<BankSlipPaymentListItemResponse>? Lancamentos { get; set; }

    [JsonPropertyName("devolucoes")]
    public List<BankSlipRefundResponse>? Devolucoes { get; set; }
}
