using System.Text.Json.Serialization;

namespace NuvTools.Payment.BancoDoBrasil.ApiClient.DTOs.Responses;

/// <summary>
/// Resposta de <c>POST /liberar-pagamentos</c>.
/// </summary>
public class ReleasePaymentResponse
{
    [JsonPropertyName("mensagemRetorno")]
    public string? MensagemRetorno { get; set; }
}
