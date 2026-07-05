using NuvTools.Common.ResultWrapper;
using NuvTools.Payment.Omie.ApiClient.DTOs.Requests;
using NuvTools.Payment.Omie.ApiClient.DTOs.Responses;

namespace NuvTools.Payment.Omie.ApiClient.Contracts;

/// <summary>
/// Interface for communication with the Omie ERP API.
/// </summary>
public interface IOmieApiClient
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

    /// <summary>
    /// Reads a Service Contract (ConsultarContrato) to obtain the negotiated unit price per service item.
    /// The contract is a price registry only — it must never be billed through.
    /// </summary>
    Task<IResult<ConsultContractResponse>> ConsultContractAsync(long omieContractCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists registered services (ListarCadastroServico) to resolve/validate a nCodServico.
    /// </summary>
    Task<IResult<ListServiceRegistrationResponse>> ListServiceRegistrationAsync(int page = 1, int recordsPerPage = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists registered categories (ListarCategorias) to resolve a service's cCodCateg to its category name.
    /// </summary>
    Task<IResult<ListCategoryRegistrationResponse>> ListCategoryRegistrationAsync(int page = 1, int recordsPerPage = 1000, string description = "", CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing Service Order (AlterarOS). Send the full item list — Omie replaces the OS items.
    /// The header must carry <see cref="DTOs.Requests.IncludeOSHeader.OsCode"/> (nCodOS) or cCodIntOS.
    /// </summary>
    Task<IResult<IncludeOSResponse>> ChangeOSAsync(IncludeOSParam param, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the current state/stage of a Service Order (ConsultarOS). Provide exactly one identifier.
    /// </summary>
    Task<IResult<ConsultOSResponse>> ConsultOSAsync(long? nCodOS = null, string? cCodIntOS = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves a Service Order to another Kanban stage (TrocarEtapaOS). Feature-flag the "billing" stage.
    /// </summary>
    Task<IResult<ChangeOSStageResponse>> ChangeOSStageAsync(ChangeOSStageParam param, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists the OS Kanban stage catalog (ListarEtapasFaturamento) to discover stage codes.
    /// </summary>
    Task<IResult<ListBillingStagesResponse>> ListBillingStagesAsync(CancellationToken cancellationToken = default);
}
