using NuvTools.Common.ResultWrapper;
using NuvTools.Payment.Contracts;
using NuvTools.Payment.Omie.ApiClient.DTOs.Requests;
using NuvTools.Payment.Omie.ApiClient.DTOs.Responses;

namespace NuvTools.Payment.Omie.ApiClient.Contracts;

/// <summary>
/// Interface for communication with the Omie ERP API. The implementation also satisfies
/// <see cref="IBankSlipBilletQuery"/> for portable billet retrieval.
/// </summary>
public interface IOmieApiClient : IBankSlipBilletQuery
{
    /// <summary>
    /// Checks whether a client exists in Omie by its code.
    /// </summary>
    Task<IResult<bool>> ConsultClientAsync(long omieClientCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the registration data of a service in Omie.
    /// </summary>
    Task<IResult<ConsultServiceRegistrationResponse>> ConsultServiceRegistrationAsync(long omieServiceCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Includes a Service Order (OS) in Omie.
    /// </summary>
    Task<IResult<IncludeOSResponse>> IncludeOSAsync(IncludeOSParam param, CancellationToken cancellationToken = default);

    /// <summary>
    /// Includes an Accounts Receivable entry in Omie.
    /// </summary>
    Task<IResult<IncludeReceivableResponse>> IncludeReceivableAsync(IncludeReceivableParam param, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates the billet of an existing Accounts Receivable entry in Omie.
    /// At least one of the two identifiers must be provided.
    /// </summary>
    Task<IResult<GenerateBilletResponse>> GenerateBilletAsync(long? nCodTitulo = null, string? cCodIntTitulo = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the billet link/details for an existing Accounts Receivable entry in Omie.
    /// Use after GenerateBillet to fetch the actual download URL, barcode, etc.
    /// </summary>
    Task<IResult<GetBilletResponse>> GetBilletAsync(long? nCodTitulo = null, string? cCodIntTitulo = null, CancellationToken cancellationToken = default);
}
