using System.Text.Json.Serialization;

namespace NuvTools.Payment.Sicoob.ApiClient.DTOs.Requests;

/// <summary>
/// Requisicao para prorrogacao de vencimento de boleto no Sicoob.
/// </summary>
public class ExtendDueDateRequest
{
    [JsonPropertyName("numeroCliente")]
    public int NumeroCliente { get; set; }

    [JsonPropertyName("codigoModalidade")]
    public int CodigoModalidade { get; set; }

    [JsonPropertyName("dataVencimento")]
    public string? DataVencimento { get; set; }
}
