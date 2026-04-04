using System.Text.Json.Serialization;

namespace NuvTools.Payment.Sicoob.ApiClient.DTOs.Responses;

/// <summary>
/// Resposta da segunda via de boleto do Sicoob.
/// </summary>
public class SecondCopyBankSlipResponse
{
    [JsonPropertyName("nossoNumero")]
    public long NossoNumero { get; set; }

    [JsonPropertyName("linhaDigitavel")]
    public string? LinhaDigitavel { get; set; }

    [JsonPropertyName("codigoBarras")]
    public string? CodigoBarras { get; set; }

    [JsonPropertyName("pdfBoleto")]
    public string? PdfBoleto { get; set; }
}
