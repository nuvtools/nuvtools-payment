using System.Text.Json.Serialization;
using NuvTools.Payment.Omie.ApiClient.DTOs.Responses;

namespace NuvTools.Payment.Omie.ApiClient.DTOs.Requests;

/// <summary>
/// Main parameter for including a Service Order in the Omie API.
/// Corresponds to the content of the "param" array in the IncluirOS request.
/// </summary>
public class IncludeOSParam
{
    // A identificação da OS vive dentro de <c>cabecalho</c>, para IncluirOS e AlterarOS. Não a coloque na raiz:
    // o Omie recusa os dois campos ali — "Tag [CCODINTOS] não faz parte da estrutura do tipo complexo
    // [osCadastro]" e, idem, "Tag [NCODOS] ...". Ao contrário do TrocarEtapaOS, que usa outro tipo complexo e
    // de fato leva nCodOS/cCodIntOS na raiz do param.
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
    // nCodOS is only sent on AlterarOS (to target the existing OS). Omitted on IncluirOS via WhenWritingNull.
    // Reusing this header for both calls avoids duplicating the full OS item/tax tree.
    [JsonPropertyName("nCodOS")]
    public long? OsCode { get; set; }

    [JsonPropertyName("cCodIntOS")]
    public required string OsIntegrationCode { get; set; }

    [JsonPropertyName("cCodParc")]
    public required string PaymentTermCode { get; set; }

    // Optional in both IncluirOS and AlterarOS. Documented domain is 10/20/30/40/50 — anything else (an extra
    // pipeline column configured in the account, for instance) is not a value this API accepts back, so on a
    // change it is better omitted than echoed.
    [JsonPropertyName("cEtapa")]
    public string? Stage { get; set; }

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

    // Tipo de Tributação dos Serviços — the registration's cIdTrib. Its domain varies per municipality.
    [JsonPropertyName("cTribServ")]
    public string? TaxationType { get; set; }

    [JsonPropertyName("cCodServLC116")]
    public string? Lc116ServiceCode { get; set; }

    // Nomenclatura Brasileira de Serviços — the registration's nIdNBS.
    [JsonPropertyName("cNbs")]
    public string? NbsCode { get; set; }

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

    // What AlterarOS should do with this item: "A" alter (the documented default), "E" exclude, "I" include
    // during the change. Meaningless on IncluirOS — leave it null there and WhenWritingNull drops it.
    [JsonPropertyName("cAcaoItem")]
    public string? ItemAction { get; set; }

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
    // All fields optional. Omie documents that a blank field on a NEW item is filled from the service
    // registration — but once the "impostos" block is sent, what is left out does not come from the cadastro
    // (and AlterarOS has no "new item" to default from). Sending only cClassTrib is what left the OS with the
    // Reforma Tributária tab (CST, indicador de operação, IBS/CBS) and the retentions empty. Build the block
    // with FromServiceRegistration so every tax field the registration carries is echoed.
    //
    // Several tags here are read-only in Omie (cFixar*, cMan*, nValor*, nBaseISS): they exist for the
    // consult/list payloads and must stay null on inclusion/change.

    /// <summary>
    /// The tax block of a service order item exactly as the service registration (<c>ConsultarCadastroServico</c>)
    /// defines it: ISS rate, PIS/COFINS/CSLL/IR/INSS retention and rates, base reductions, ISS deduction and the
    /// Reforma Tributária fields (CST, classification, operation indicator, IBS/CBS rates and reductions).
    /// <para>
    /// The tag names differ between the two Omie types (<c>cRetPIS</c> → <c>cRetemPIS</c>, <c>nAliqIR</c> →
    /// <c>nAliqIRRF</c>, <c>cCstIbsCbs</c> → <c>cCST</c>, <c>cIndOper</c> → <c>cIndOPer</c>, <c>nRedBasePIS</c> →
    /// <c>nAliqRedBasePIS</c>) — that translation is why this lives in the client. Blank codes stay null, so Omie
    /// keeps its own default instead of receiving an empty value.
    /// </para>
    /// <para>
    /// The ISS retention is not here: on the OS it belongs to the item (<c>cRetemISS</c>), not to <c>impostos</c>.
    /// The registration's PIS/COFINS apuração CST (<c>cCstPIS</c>/<c>cCstCOFINS</c>) has no counterpart on the
    /// service order item, so it cannot be sent.
    /// </para>
    /// </summary>
    public static IncludeOSTaxes FromServiceRegistration(ConsultServiceTaxes taxes)
    {
        ArgumentNullException.ThrowIfNull(taxes);

        return new IncludeOSTaxes
        {
            IssRate = taxes.IssRate,

            WithholdPis = NullIfBlank(taxes.WithholdPis),
            PisRate = taxes.PisRate,
            PisBaseReductionRate = taxes.PisBaseReduction,

            WithholdCofins = NullIfBlank(taxes.WithholdCofins),
            CofinsRate = taxes.CofinsRate,
            CofinsBaseReductionRate = taxes.CofinsBaseReduction,

            WithholdCsll = NullIfBlank(taxes.WithholdCsll),
            CsllRate = taxes.CsllRate,

            WithholdIrrf = NullIfBlank(taxes.WithholdIr),
            IrrfRate = taxes.IrRate,

            WithholdInss = NullIfBlank(taxes.WithholdInss),
            InssRate = taxes.InssRate,
            InssBaseReductionRate = taxes.InssBaseReduction,

            DeductIss = taxes.DeductIss,

            IbsCbsCst = NullIfBlank(taxes.IbsCbsCst),
            TaxClassification = NullIfBlank(taxes.TaxClassification),
            OperationIndicator = NullIfBlank(taxes.OperationIndicator),
            IbsMunRate = taxes.IbsMunRate,
            IbsMunReductionPercentage = taxes.IbsMunReductionPercentage,
            IbsUfRate = taxes.IbsUfRate,
            IbsUfReductionPercentage = taxes.IbsUfReductionPercentage,
            CbsRate = taxes.CbsRate,
            CbsReductionPercentage = taxes.CbsReductionPercentage
        };
    }

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    // Código da Situação Tributária IBS/CBS — the registration's cCstIbsCbs.
    [JsonPropertyName("cCST")]
    public string? IbsCbsCst { get; set; }

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

    // The registration calls it nAliqIR; on the OS item it is nAliqIRRF.
    [JsonPropertyName("nAliqIRRF")]
    public decimal? IrrfRate { get; set; }

    [JsonPropertyName("nAliqRedBaseCOFINS")]
    public decimal? CofinsBaseReductionRate { get; set; }

    [JsonPropertyName("nAliqRedBaseINSS")]
    public decimal? InssBaseReductionRate { get; set; }

    [JsonPropertyName("nAliqRedBasePIS")]
    public decimal? PisBaseReductionRate { get; set; }

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
