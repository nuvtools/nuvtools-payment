using System.Text.Json.Serialization;

namespace NuvTools.Payment.Omie.ApiClient.DTOs.Responses;

/// <summary>
/// Response of ObterBoleto — fetches the billet link, identifiers and bank-side
/// details after GerarBoleto. Note Omie uses "cDesStatus" (without the second "c")
/// in the billet endpoints.
/// </summary>
public class GetBilletResponse
{
    [JsonPropertyName("nCodTitulo")]
    public long TitleCode { get; set; }

    [JsonPropertyName("cCodIntTitulo")]
    public string? TitleIntegrationCode { get; set; }

    [JsonPropertyName("cCodStatus")]
    public string? StatusCode { get; set; }

    [JsonPropertyName("cDesStatus")]
    public string? StatusDescription { get; set; }

    [JsonPropertyName("cLinkBoleto")]
    public string? BilletLink { get; set; }

    [JsonPropertyName("cNumBoleto")]
    public string? BilletNumber { get; set; }

    [JsonPropertyName("cCodBarras")]
    public string? Barcode { get; set; }

    [JsonPropertyName("cNumBancario")]
    public string? BankNumber { get; set; }

    [JsonPropertyName("dDtEmBol")]
    public string? BilletIssueDate { get; set; }

    [JsonPropertyName("dDtVencBol")]
    public string? BilletDueDate { get; set; }

    [JsonPropertyName("nPerJuros")]
    public decimal InterestPercentage { get; set; }

    [JsonPropertyName("nPerMulta")]
    public decimal FinePercentage { get; set; }

    [JsonPropertyName("dDescontoCond1")]
    public string? DiscountDate1 { get; set; }

    [JsonPropertyName("vDescontoCond1")]
    public decimal DiscountValue1 { get; set; }

    [JsonPropertyName("dDescontoCond2")]
    public string? DiscountDate2 { get; set; }

    [JsonPropertyName("vDescontoCond2")]
    public decimal DiscountValue2 { get; set; }

    [JsonPropertyName("dDescontoCond3")]
    public string? DiscountDate3 { get; set; }

    [JsonPropertyName("vDescontoCond3")]
    public decimal DiscountValue3 { get; set; }
}
