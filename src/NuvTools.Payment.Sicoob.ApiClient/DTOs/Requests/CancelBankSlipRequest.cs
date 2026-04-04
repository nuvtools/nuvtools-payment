using System.Text.Json.Serialization;

namespace NuvTools.Payment.Sicoob.ApiClient.DTOs.Requests;

/// <summary>
/// Requisicao para cancelamento de boleto no Sicoob.
/// </summary>
public class CancelBankSlipRequest
{
    [JsonPropertyName("numeroCliente")]
    public int NumeroCliente { get; set; }

    [JsonPropertyName("codigoModalidade")]
    public int CodigoModalidade { get; set; }
}
