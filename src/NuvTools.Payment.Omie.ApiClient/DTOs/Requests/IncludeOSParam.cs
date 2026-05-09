using System.Text.Json.Serialization;

namespace NuvTools.Payment.Omie.ApiClient.DTOs.Requests;

/// <summary>
/// Main parameter for including a Service Order in the Omie API.
/// Corresponds to the content of the "param" array in the IncluirOS request.
/// </summary>
public class IncludeOSParam
{
    [JsonPropertyName("cabecalho")]
    public required IncludeOSHeader Header { get; set; }

    [JsonPropertyName("informacoesAdicionais")]
    public required IncludeOSAdditionalInfo AdditionalInfo { get; set; }

    [JsonPropertyName("parcelas")]
    public required IncludeOSInstallment[] Installments { get; set; }

    [JsonPropertyName("servicosPrestados")]
    public required IncludeOSProvidedService[] ProvidedServices { get; set; }
}

public class IncludeOSHeader
{
    [JsonPropertyName("cCodIntOS")]
    public required string OsIntegrationCode { get; set; }

    [JsonPropertyName("cCodParc")]
    public required string PaymentTermCode { get; set; }

    [JsonPropertyName("cEtapa")]
    public required string Stage { get; set; }

    [JsonPropertyName("dDtPrevisao")]
    public required string ForecastDate { get; set; }

    [JsonPropertyName("nCodCli")]
    public required long ClientCode { get; set; }

    [JsonPropertyName("nQtdeParc")]
    public required int InstallmentCount { get; set; }

    [JsonPropertyName("nValorTotal")]
    public decimal? TotalValue { get; set; }

    [JsonPropertyName("nValorTotalImpRet")]
    public decimal? TotalRetainedTaxValue { get; set; }
}

public class IncludeOSAdditionalInfo
{
    // Optional. When omitted, Omie uses the empresa's default service provision city.
    // Sending an IBGE code that the empresa hasn't enabled returns "Cidade não cadastrada
    // para o Código [...]". Letting Omie pick the default is the safest path.
    [JsonPropertyName("cCidPrestServ")]
    public string? ServiceProvisionCity { get; set; }

    [JsonPropertyName("cCodCateg")]
    public required string CategoryCode { get; set; }

    [JsonPropertyName("cContato")]
    public string? Contact { get; set; }

    [JsonPropertyName("cDadosAdicNF")]
    public required string InvoiceAdditionalData { get; set; }

    [JsonPropertyName("nCodCC")]
    public required long CheckingAccountCode { get; set; }
}

public class IncludeOSInstallment
{
    [JsonPropertyName("dDtVenc")]
    public required string DueDate { get; set; }

    [JsonPropertyName("nDias")]
    public required int Days { get; set; }

    [JsonPropertyName("nParcela")]
    public required int InstallmentNumber { get; set; }

    [JsonPropertyName("nPercentual")]
    public required decimal Percentage { get; set; }

    [JsonPropertyName("nValor")]
    public decimal? Value { get; set; }
}

public class IncludeOSProvidedService
{
    [JsonPropertyName("nCodServico")]
    public required long ServiceCode { get; set; }

    [JsonPropertyName("cCodServLC116")]
    public string? Lc116ServiceCode { get; set; }

    [JsonPropertyName("cCodServMun")]
    public string? MunicipalServiceCode { get; set; }

    [JsonPropertyName("cDescServ")]
    public string? ServiceDescription { get; set; }

    [JsonPropertyName("cRetemISS")]
    public string? WithholdIss { get; set; }

    [JsonPropertyName("cCodCategItem")]
    public string? ItemCategoryCode { get; set; }

    [JsonPropertyName("cNaoGerarFinanceiro")]
    public string? DoNotGenerateFinancial { get; set; }

    [JsonPropertyName("cReembolso")]
    public string? Reimbursement { get; set; }

    [JsonPropertyName("cTpDesconto")]
    public string? DiscountType { get; set; }

    [JsonPropertyName("nAliqDesconto")]
    public decimal? DiscountRate { get; set; }

    [JsonPropertyName("nSeqItem")]
    public required int ItemSequence { get; set; }

    [JsonPropertyName("nValorAcrescimos")]
    public decimal? AdditionsValue { get; set; }

    [JsonPropertyName("nValorDesconto")]
    public decimal? DiscountValue { get; set; }

    [JsonPropertyName("nValorOutrasRetencoes")]
    public decimal? OtherRetentionsValue { get; set; }

    [JsonPropertyName("nQtde")]
    public required decimal Quantity { get; set; }

    [JsonPropertyName("nValUnit")]
    public decimal? UnitValue { get; set; }

    [JsonPropertyName("impostos")]
    public IncludeOSTaxes? Taxes { get; set; }
}

public class IncludeOSTaxes
{
    // All fields optional. When sent, Omie uses these values; when omitted, Omie
    // pulls from the service registration. Pre-filling cClassTrib + alíquotas + retentions
    // from the consult is what makes the OS show tributação inherited from the cadastro
    // (instead of being treated as a free-form item).

    [JsonPropertyName("cClassTrib")]
    public string? TaxClassification { get; set; }

    [JsonPropertyName("cFixarCOFINS")]
    public string? FixCofins { get; set; }

    [JsonPropertyName("cFixarCSLL")]
    public string? FixCsll { get; set; }

    [JsonPropertyName("cFixarINSS")]
    public string? FixInss { get; set; }

    [JsonPropertyName("cFixarIRRF")]
    public string? FixIrrf { get; set; }

    [JsonPropertyName("cFixarISS")]
    public string? FixIss { get; set; }

    [JsonPropertyName("cFixarPIS")]
    public string? FixPis { get; set; }

    [JsonPropertyName("cIndOPer")]
    public string? OperationIndicator { get; set; }

    [JsonPropertyName("cManCbs")]
    public string? ManualCbs { get; set; }

    [JsonPropertyName("cManIbsMun")]
    public string? ManualIbsMun { get; set; }

    [JsonPropertyName("cManIbsUf")]
    public string? ManualIbsUf { get; set; }

    [JsonPropertyName("cRetemCOFINS")]
    public string? WithholdCofins { get; set; }

    [JsonPropertyName("cRetemCSLL")]
    public string? WithholdCsll { get; set; }

    [JsonPropertyName("cRetemINSS")]
    public string? WithholdInss { get; set; }

    [JsonPropertyName("cRetemIRRF")]
    public string? WithholdIrrf { get; set; }

    [JsonPropertyName("cRetemPIS")]
    public string? WithholdPis { get; set; }

    [JsonPropertyName("lDeduzISS")]
    public bool? DeductIss { get; set; }

    [JsonPropertyName("nAliqCOFINS")]
    public decimal? CofinsRate { get; set; }

    [JsonPropertyName("nAliqCSLL")]
    public decimal? CsllRate { get; set; }

    [JsonPropertyName("nAliqCbs")]
    public decimal? CbsRate { get; set; }

    [JsonPropertyName("nAliqINSS")]
    public decimal? InssRate { get; set; }

    [JsonPropertyName("nAliqISS")]
    public decimal? IssRate { get; set; }

    [JsonPropertyName("nAliqIbsMun")]
    public decimal? IbsMunRate { get; set; }

    [JsonPropertyName("nAliqIbsUf")]
    public decimal? IbsUfRate { get; set; }

    [JsonPropertyName("nAliqPIS")]
    public decimal? PisRate { get; set; }

    [JsonPropertyName("nBaseISS")]
    public decimal? IssBase { get; set; }

    [JsonPropertyName("nPercReducaoCbs")]
    public decimal? CbsReductionPercentage { get; set; }

    [JsonPropertyName("nPercReducaoIbsMun")]
    public decimal? IbsMunReductionPercentage { get; set; }

    [JsonPropertyName("nPercReducaoIbsUf")]
    public decimal? IbsUfReductionPercentage { get; set; }

    [JsonPropertyName("nTotDeducao")]
    public decimal? TotalDeduction { get; set; }

    [JsonPropertyName("nValorCOFINS")]
    public decimal? CofinsValue { get; set; }

    [JsonPropertyName("nValorCSLL")]
    public decimal? CsllValue { get; set; }

    [JsonPropertyName("nValorCbs")]
    public decimal? CbsValue { get; set; }

    [JsonPropertyName("nValorDeducao")]
    public decimal? DeductionValue { get; set; }

    [JsonPropertyName("nValorDeducaoIRRF")]
    public decimal? IrrfDeductionValue { get; set; }

    [JsonPropertyName("nValorINSS")]
    public decimal? InssValue { get; set; }

    [JsonPropertyName("nValorIRRF")]
    public decimal? IrrfValue { get; set; }

    [JsonPropertyName("nValorISS")]
    public decimal? IssValue { get; set; }

    [JsonPropertyName("nValorIbsMun")]
    public decimal? IbsMunValue { get; set; }

    [JsonPropertyName("nValorIbsUf")]
    public decimal? IbsUfValue { get; set; }

    [JsonPropertyName("nValorPIS")]
    public decimal? PisValue { get; set; }
}
