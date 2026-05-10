using NuvTools.Common.ResultWrapper;
using NuvTools.Payment.BancoDoBrasil.ApiClient.DTOs.Requests;
using NuvTools.Payment.BancoDoBrasil.ApiClient.DTOs.Responses;
using NuvTools.Payment.Contracts;

namespace NuvTools.Payment.BancoDoBrasil.ApiClient.Contracts;

/// <summary>
/// Banco do Brasil bank slip batch payment API contract — exposes the raw provider DTOs.
/// Implementations also satisfy <see cref="IBankSlipBatchPaymentClient"/> for portable callers.
/// </summary>
public interface IBbBankSlipPaymentApiClient : IBankSlipBatchPaymentClient
{
    Task<IResult<BbAccessTokenResponse>> GenerateAccessTokenAsync(string scope, CancellationToken cancellationToken = default);

    Task<IResult<CreateBatchPaymentResponse>> CreateBatchPaymentAsync(CreateBatchPaymentRequest request, CancellationToken cancellationToken = default);

    Task<IResult<BankSlipPaymentResponse>> GetPaymentAsync(long paymentId, string agency, string account, string digit, CancellationToken cancellationToken = default);
}
