namespace NuvTools.Payment.Omie.ApiClient.DTOs.Responses;

/// <summary>
/// The full registration of a service (<c>ConsultarCadastroServico</c>). The listing does not carry these fields and
/// including a service order requires them on every item: LC 116 service code, municipal service code and tax
/// classification. None of them can be invented — when they are missing from the Omie registration, the caller
/// should stop and say which service to fix.
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
    string? TaxClassification);
