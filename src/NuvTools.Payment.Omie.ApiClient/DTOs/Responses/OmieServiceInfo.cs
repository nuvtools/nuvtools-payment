namespace NuvTools.Payment.Omie.ApiClient.DTOs.Responses;

/// <summary>
/// A service of the Omie catalog, as <c>ListarCadastroServico</c> returns it.
/// <para>
/// The two codes exist and are not interchangeable: <see cref="Code"/> is the business code (<c>cCodigo</c>, e.g.
/// "SRV00016"), the one applications store in their own configuration, while <see cref="ServiceCode"/> is Omie's
/// internal identifier (<c>nCodServico</c>), the only one accepted when building service order items. Translating
/// one into the other is the reason the whole catalog is read.
/// </para>
/// </summary>
/// <param name="ServiceCode">Omie's internal service identifier (<c>nCodServico</c>).</param>
/// <param name="Code">Business code (<c>cCodigo</c>).</param>
/// <param name="Description">Service description.</param>
/// <param name="UnitPrice">Registered unit price.</param>
public record OmieServiceInfo(long ServiceCode, string Code, string? Description, decimal UnitPrice);
