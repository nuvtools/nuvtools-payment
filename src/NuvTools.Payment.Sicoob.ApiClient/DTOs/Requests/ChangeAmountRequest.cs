using System.Text.Json.Serialization;

namespace NuvTools.Payment.Sicoob.ApiClient.DTOs.Requests;

/// <summary>
/// Requisicao para alteracao de valor de boleto no Sicoob.
/// </summary>
public class ChangeAmountRequest
{
    [JsonPropertyName("numeroCliente")]
    public int NumeroCliente { get; set; }

    [JsonPropertyName("codigoModalidade")]
    public int CodigoModalidade { get; set; }

    [JsonPropertyName("valor")]
    public decimal Valor { get; set; }
}
