using System.Text.Json.Serialization;

namespace NuvTools.Payment.BancoDoBrasil.ApiClient.DTOs.Responses;

/// <summary>
/// Item de devolução na consulta de um lançamento de guia.
/// </summary>
public class GuiaPaymentDevolucao
{
    [JsonPropertyName("codigoMotivo")]
    public int CodigoMotivo { get; set; }

    [JsonPropertyName("dataDevolucao")]
    public long? DataDevolucao { get; set; }

    [JsonPropertyName("valorDevolucao")]
    public decimal? ValorDevolucao { get; set; }
}
