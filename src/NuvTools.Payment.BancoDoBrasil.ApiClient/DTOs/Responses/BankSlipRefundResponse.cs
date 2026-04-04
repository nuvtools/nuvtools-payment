using System.Text.Json.Serialization;

namespace NuvTools.Payment.BancoDoBrasil.ApiClient.DTOs.Responses;

/// <summary>
/// Item de devolucao na resposta de consulta de pagamento do BB.
/// </summary>
public class BankSlipRefundResponse
{
    [JsonPropertyName("codigoMotivoDevolucao")]
    public int CodigoMotivoDevolucao { get; set; }

    [JsonPropertyName("descricaoMotivoDevolucao")]
    public string? DescricaoMotivoDevolucao { get; set; }

    [JsonPropertyName("valorDevolucao")]
    public decimal ValorDevolucao { get; set; }

    [JsonPropertyName("dataDevolucao")]
    public string? DataDevolucao { get; set; }
}
