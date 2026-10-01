using System.Text.Json.Serialization;

namespace NuvTools.Payment.BancoDoBrasil.ApiClient.DTOs.Responses;

/// <summary>
/// Resposta de <c>POST /lotes-guias-codigo-barras</c> e de <c>GET /lotes-guias-codigo-barras/{idRequisicao}/solicitacao</c>.
/// </summary>
public class GuiaBatchPaymentResponse
{
    [JsonPropertyName("numeroRequisicao")]
    public long NumeroRequisicao { get; set; }

    /// <summary>Estado da requisição (1 = consistente; 4 = pendente de liberação; 6 = processada; 7 = rejeitada; etc.).</summary>
    [JsonPropertyName("codigoEstado")]
    public int CodigoEstado { get; set; }

    [JsonPropertyName("quantidadeLancamentos")]
    public int QuantidadeLancamentos { get; set; }

    [JsonPropertyName("valorLancamentos")]
    public decimal ValorLancamentos { get; set; }

    [JsonPropertyName("quantidadeLancamentosValidos")]
    public int QuantidadeLancamentosValidos { get; set; }

    [JsonPropertyName("valorLancamentosValidos")]
    public decimal ValorLancamentosValidos { get; set; }

    [JsonPropertyName("lancamentos")]
    public List<GuiaBatchPaymentResponseItem>? Lancamentos { get; set; }
}
