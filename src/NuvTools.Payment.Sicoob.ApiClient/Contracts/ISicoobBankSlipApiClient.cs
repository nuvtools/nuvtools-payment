using NuvTools.Common.ResultWrapper;
using NuvTools.Payment.Sicoob.ApiClient.DTOs.Requests;
using NuvTools.Payment.Sicoob.ApiClient.DTOs.Responses;

namespace NuvTools.Payment.Sicoob.ApiClient.Contracts;

/// <summary>
/// Sicoob bank slip API contract.
/// </summary>
public interface ISicoobBankSlipApiClient
{
    Task<IResult<BankSlipResponse>> GetBankSlipAsync(int customerNumber, int modalityCode, long ourNumber, CancellationToken cancellationToken = default);

    Task<IResult<List<BankSlipResponse>>> ListBankSlipsByPeriodAsync(string payerDocument, int customerNumber, DateOnly startDate, DateOnly endDate, int statusCode, CancellationToken cancellationToken = default);

    Task<IResult<SecondCopyBankSlipResponse>> GetSecondCopyAsync(int customerNumber, int modalityCode, long ourNumber, CancellationToken cancellationToken = default);

    Task<IResult<CreateBankSlipResponse>> CreateBankSlipAsync(CreateBankSlipRequest request, CancellationToken cancellationToken = default);

    Task<IResult> CancelBankSlipAsync(long ourNumber, CancelBankSlipRequest request, CancellationToken cancellationToken = default);

    Task<IResult> ExtendDueDateAsync(long ourNumber, ExtendDueDateRequest request, CancellationToken cancellationToken = default);

    Task<IResult> ChangeAmountAsync(long ourNumber, ChangeAmountRequest request, CancellationToken cancellationToken = default);
}
