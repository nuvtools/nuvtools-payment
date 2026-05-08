using System.Text.Json.Serialization;

namespace NuvTools.Payment.Omie.ApiClient.DTOs.Responses;

/// <summary>
/// Response of ConsultarCadastroServico — modeled from the actual Omie payload.
/// The unit price (<c>nPrecoUnit</c>) lives in the <see cref="Header"/> block;
/// there is no separate "valores" block.
/// </summary>
public class ConsultServiceRegistrationResponse
{
    [JsonPropertyName("intListar")]
    public ConsultServiceIntList? IntList { get; set; }

    [JsonPropertyName("cabecalho")]
    public ConsultServiceHeader? Header { get; set; }

    [JsonPropertyName("descricao")]
    public ConsultServiceDescription? Description { get; set; }

    [JsonPropertyName("impostos")]
    public ConsultServiceTaxes? Taxes { get; set; }
}

public class ConsultServiceIntList
{
    [JsonPropertyName("cCodIntServ")]
    public string? IntegrationCode { get; set; }

    [JsonPropertyName("nCodServ")]
    public long? Code { get; set; }
}

public class ConsultServiceHeader
{
    [JsonPropertyName("cCodigo")]
    public string? Code { get; set; }

    [JsonPropertyName("cCodCateg")]
    public string? CategoryCode { get; set; }

    [JsonPropertyName("cCodLC116")]
    public string? Lc116Code { get; set; }

    [JsonPropertyName("cCodServMun")]
    public string? MunicipalServiceCode { get; set; }

    [JsonPropertyName("cDescricao")]
    public string? Description { get; set; }

    [JsonPropertyName("cIdTrib")]
    public string? TaxId { get; set; }

    [JsonPropertyName("cTipoDesc")]
    public string? DiscountType { get; set; }

    [JsonPropertyName("nAliqDesc")]
    public decimal DiscountRate { get; set; }

    [JsonPropertyName("nIdNBS")]
    public string? NbsId { get; set; }

    [JsonPropertyName("nPrecoUnit")]
    public decimal UnitPrice { get; set; }

    [JsonPropertyName("nValorDesc")]
    public decimal DiscountValue { get; set; }
}

public class ConsultServiceDescription
{
    [JsonPropertyName("cDescrCompleta")]
    public string? FullDescription { get; set; }
}

public class ConsultServiceTaxes
{
    [JsonPropertyName("cClassTrib")]
    public string? TaxClassification { get; set; }

    [JsonPropertyName("cCstIbsCbs")]
    public string? IbsCbsCst { get; set; }

    [JsonPropertyName("cIndOper")]
    public string? OperationIndicator { get; set; }

    [JsonPropertyName("cRetCOFINS")]
    public string? WithholdCofins { get; set; }

    [JsonPropertyName("cRetCSLL")]
    public string? WithholdCsll { get; set; }

    [JsonPropertyName("cRetINSS")]
    public string? WithholdInss { get; set; }

    [JsonPropertyName("cRetIR")]
    public string? WithholdIr { get; set; }

    [JsonPropertyName("cRetISS")]
    public string? WithholdIss { get; set; }

    [JsonPropertyName("cRetPIS")]
    public string? WithholdPis { get; set; }

    [JsonPropertyName("lDeduzISS")]
    public bool DeductIss { get; set; }

    [JsonPropertyName("nAliqCOFINS")]
    public decimal CofinsRate { get; set; }

    [JsonPropertyName("nAliqCSLL")]
    public decimal CsllRate { get; set; }

    [JsonPropertyName("nAliqCbs")]
    public decimal CbsRate { get; set; }

    [JsonPropertyName("nAliqINSS")]
    public decimal InssRate { get; set; }

    [JsonPropertyName("nAliqIR")]
    public decimal IrRate { get; set; }

    [JsonPropertyName("nAliqISS")]
    public decimal IssRate { get; set; }

    [JsonPropertyName("nAliqIbsMun")]
    public decimal IbsMunRate { get; set; }

    [JsonPropertyName("nAliqIbsUf")]
    public decimal IbsUfRate { get; set; }

    [JsonPropertyName("nAliqPIS")]
    public decimal PisRate { get; set; }

    [JsonPropertyName("nPercReducaoCbs")]
    public decimal CbsReductionPercentage { get; set; }

    [JsonPropertyName("nPercReducaoIbsMun")]
    public decimal IbsMunReductionPercentage { get; set; }

    [JsonPropertyName("nPercReducaoIbsUf")]
    public decimal IbsUfReductionPercentage { get; set; }

    [JsonPropertyName("nRedBaseCOFINS")]
    public decimal CofinsBaseReduction { get; set; }

    [JsonPropertyName("nRedBaseINSS")]
    public decimal InssBaseReduction { get; set; }

    [JsonPropertyName("nRedBasePIS")]
    public decimal PisBaseReduction { get; set; }
}
