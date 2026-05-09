using NuvTools.Payment.Omie.ApiClient.DTOs.Requests;
using NuvTools.Payment.Omie.ApiClient.DTOs.Responses;

namespace NuvTools.Payment.Omie.ApiClient.Contracts;

/// <summary>
/// Interface for communication with the Omie API.
/// </summary>
public interface IOmieApiClient
{
    /// <summary>
    /// Checks whether a client exists in Omie by its code.
    /// </summary>
    Task<OmieApiResult<bool>> ConsultClientAsync(long omieClientCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the registration data of a service in Omie.
    /// </summary>
    Task<OmieApiResult<ConsultServiceRegistrationResponse>> ConsultServiceRegistrationAsync(long omieServiceCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Includes a Service Order (OS) in Omie.
    /// </summary>
    Task<OmieApiResult<IncludeOSResponse>> IncludeOSAsync(IncludeOSParam param, CancellationToken cancellationToken = default);

    /// <summary>
    /// Includes an Accounts Receivable entry in Omie.
    /// </summary>
    Task<OmieApiResult<IncludeReceivableResponse>> IncludeReceivableAsync(IncludeReceivableParam param, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates the billet of an existing Accounts Receivable entry in Omie.
    /// At least one of the two identifiers must be provided.
    /// </summary>
    Task<OmieApiResult<GenerateBilletResponse>> GenerateBilletAsync(long? nCodTitulo = null, string? cCodIntTitulo = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the billet link/details for an existing Accounts Receivable entry in Omie.
    /// Use after GenerateBillet to fetch the actual download URL, barcode, etc.
    /// </summary>
    Task<OmieApiResult<GetBilletResponse>> GetBilletAsync(long? nCodTitulo = null, string? cCodIntTitulo = null, CancellationToken cancellationToken = default);
}
