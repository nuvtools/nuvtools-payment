using System.Text.Json.Serialization;

namespace NuvTools.Payment.Omie.ApiClient.DTOs.Responses;

public class ConsultarCadastroServicoResponse
{
    [JsonPropertyName("cabecalho")]
    public ConsultarServicoCabecalho? Cabecalho { get; set; }

    [JsonPropertyName("impostos")]
    public ConsultarServicoImpostos? Impostos { get; set; }
}

public class ConsultarServicoCabecalho
{
    [JsonPropertyName("cCodCateg")]
    public string? CCodCateg { get; set; }

    [JsonPropertyName("cDescricao")]
    public string? CDescricao { get; set; }

    [JsonPropertyName("cCodLC116")]
    public string? CCodLC116 { get; set; }

    [JsonPropertyName("cCodServMun")]
    public string? CCodServMun { get; set; }
}

public class ConsultarServicoImpostos
{
    [JsonPropertyName("cRetISS")]
    public string? CRetISS { get; set; }

    [JsonPropertyName("nAliqISS")]
    public decimal NAliqISS { get; set; }

    [JsonPropertyName("cClassTrib")]
    public string? CClassTrib { get; set; }

    [JsonPropertyName("cIndOper")]
    public string? CIndOper { get; set; }

    [JsonPropertyName("lDeduzISS")]
    public bool LDeduzISS { get; set; }

    [JsonPropertyName("nAliqCOFINS")]
    public decimal NAliqCOFINS { get; set; }

    [JsonPropertyName("nAliqCSLL")]
    public decimal NAliqCSLL { get; set; }

    [JsonPropertyName("nAliqCbs")]
    public decimal NAliqCbs { get; set; }

    [JsonPropertyName("nAliqINSS")]
    public decimal NAliqINSS { get; set; }

    [JsonPropertyName("nAliqIbsMun")]
    public decimal NAliqIbsMun { get; set; }

    [JsonPropertyName("nAliqIbsUf")]
    public decimal NAliqIbsUf { get; set; }

    [JsonPropertyName("nAliqPIS")]
    public decimal NAliqPIS { get; set; }

    [JsonPropertyName("nAliqIRRF")]
    public decimal NAliqIRRF { get; set; }

    [JsonPropertyName("nPercReducaoCbs")]
    public decimal NPercReducaoCbs { get; set; }

    [JsonPropertyName("nPercReducaoIbsMun")]
    public decimal NPercReducaoIbsMun { get; set; }

    [JsonPropertyName("nPercReducaoIbsUf")]
    public decimal NPercReducaoIbsUf { get; set; }
}
