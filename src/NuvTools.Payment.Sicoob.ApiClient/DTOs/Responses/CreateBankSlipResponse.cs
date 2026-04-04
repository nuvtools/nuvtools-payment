using System.Text.Json.Serialization;

namespace NuvTools.Payment.Sicoob.ApiClient.DTOs.Responses;

/// <summary>
/// Resposta da criacao de boleto no Sicoob.
/// </summary>
public class CreateBankSlipResponse
{
    [JsonPropertyName("nossoNumero")]
    public long NossoNumero { get; set; }

    [JsonPropertyName("linhaDigitavel")]
    public string? LinhaDigitavel { get; set; }

    [JsonPropertyName("codigoBarras")]
    public string? CodigoBarras { get; set; }

    [JsonPropertyName("situacaoBoleto")]
    public int SituacaoBoleto { get; set; }
}
