using System.Text.Json.Serialization;

namespace NuvTools.Payment.BancoDoBrasil.ApiClient.DTOs.Requests;

/// <summary>
/// Requisição de <c>POST /liberar-pagamentos</c> — libera (autoriza) uma requisição de pagamento já criada,
/// sem a qual o lote não é efetivado (permanece pendente de ação do conveniado).
/// </summary>
public class ReleasePaymentRequest
{
    [JsonPropertyName("numeroRequisicao")]
    public long NumeroRequisicao { get; set; }

    /// <summary>Confirmação/concordância quanto à tarifa de antecipação (ex.: "S").</summary>
    [JsonPropertyName("indicadorFloat")]
    public string? IndicadorFloat { get; set; }
}
