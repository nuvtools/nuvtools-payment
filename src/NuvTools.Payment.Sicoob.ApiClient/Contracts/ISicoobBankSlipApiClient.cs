using NuvTools.Payment.Sicoob.ApiClient.DTOs.Requests;
using NuvTools.Payment.Sicoob.ApiClient.DTOs.Responses;

namespace NuvTools.Payment.Sicoob.ApiClient.Contracts;

/// <summary>
/// Interface para comunicacao com a API de boletos do Sicoob.
/// </summary>
public interface ISicoobBankSlipApiClient
{
    /// <summary>
    /// Consulta um boleto pelo nosso numero.
    /// </summary>
    Task<SicoobApiResult<BankSlipResponse>> GetBankSlipAsync(int customerNumber, int modalityCode, long ourNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista boletos por periodo.
    /// </summary>
    Task<SicoobApiResult<List<BankSlipResponse>>> ListBankSlipsByPeriodAsync(string payerDocument, int customerNumber, DateOnly startDate, DateOnly endDate, int statusCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém a segunda via de um boleto.
    /// </summary>
    Task<SicoobApiResult<SecondCopyBankSlipResponse>> GetSecondCopyAsync(int customerNumber, int modalityCode, long ourNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cria um novo boleto.
    /// </summary>
    Task<SicoobApiResult<CreateBankSlipResponse>> CreateBankSlipAsync(CreateBankSlipRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancela um boleto existente.
    /// </summary>
    Task<SicoobApiResult<bool>> CancelBankSlipAsync(long ourNumber, CancelBankSlipRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Prorroga a data de vencimento de um boleto.
    /// </summary>
    Task<SicoobApiResult<bool>> ExtendDueDateAsync(long ourNumber, ExtendDueDateRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Altera o valor de um boleto.
    /// </summary>
    Task<SicoobApiResult<bool>> ChangeAmountAsync(long ourNumber, ChangeAmountRequest request, CancellationToken cancellationToken = default);
}
