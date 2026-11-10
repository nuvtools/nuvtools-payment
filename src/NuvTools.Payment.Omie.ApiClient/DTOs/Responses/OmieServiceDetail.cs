namespace NuvTools.Payment.Omie.ApiClient.DTOs.Responses;

/// <summary>
/// The full registration of a service (<c>ConsultarCadastroServico</c>). The listing does not carry these fields and
/// including a service order requires them on every item: LC 116 service code, municipal service code and tax
/// classification. None of them can be invented — when they are missing from the Omie registration, the caller
/// should stop and say which service to fix.
/// <para>
/// Besides the positional fields, the record carries the rest of what the registration screen holds (tipo de
/// tributação, NBS, detailed description, ISS retention and the whole <c>impostos</c> block) so a service
/// order item can be built as the registration defines it — see <c>IncludeOSTaxes.FromServiceRegistration</c>.
/// </para>
/// </summary>
/// <param name="ServiceCode">Omie's internal service identifier (<c>nCodServico</c>).</param>
/// <param name="Code">Business code (<c>cCodigo</c>).</param>
/// <param name="Description">Service description.</param>
/// <param name="UnitPrice">Registered unit price.</param>
/// <param name="Lc116Code">LC 116 service code.</param>
/// <param name="MunicipalServiceCode">Service code within the municipality.</param>
/// <param name="TaxClassification">Tax classification of the service.</param>
public record OmieServiceDetail(
    long ServiceCode,
    string? Code,
    string? Description,
    decimal UnitPrice,
    string? Lc116Code,
    string? MunicipalServiceCode,
    string? TaxClassification)
{
    /// <summary>Tipo de Tributação dos Serviços (<c>cIdTrib</c>) — on the OS item it is <c>cTribServ</c>.</summary>
    public string? TaxationType { get; init; }

    /// <summary>NBS code (<c>nIdNBS</c>) — on the OS item it is <c>cNbs</c>.</summary>
    public string? NbsCode { get; init; }

    /// <summary>Descrição Detalhada do Serviço (<c>descricao.cDescrCompleta</c>).</summary>
    public string? FullDescription { get; init; }

    /// <summary>
    /// The registration's tax block (<c>impostos</c>): ISS, retentions and rates of PIS/COFINS/CSLL/IR/INSS and the
    /// Reforma Tributária fields. Null only when Omie returned no tax block at all.
    /// </summary>
    public ConsultServiceTaxes? Taxes { get; init; }
}
