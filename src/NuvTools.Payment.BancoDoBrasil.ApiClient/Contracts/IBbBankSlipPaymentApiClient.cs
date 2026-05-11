using NuvTools.Common.ResultWrapper;
using NuvTools.Payment.BancoDoBrasil.ApiClient.DTOs.Requests;
using NuvTools.Payment.BancoDoBrasil.ApiClient.DTOs.Responses;

namespace NuvTools.Payment.BancoDoBrasil.ApiClient.Contracts;

/// <summary>
/// Banco do Brasil bank slip batch payment API contract.
/// </summary>
public interface IBbBankSlipPaymentApiClient
{
    Task<IResult<BbAccessTokenResponse>> GenerateAccessTokenAsync(string scope, CancellationToken cancellationToken = default);

    Task<IResult<CreateBatchPaymentResponse>> CreateBatchPaymentAsync(CreateBatchPaymentRequest request, CancellationToken cancellationToken = default);

    Task<IResult<BankSlipPaymentResponse>> GetPaymentAsync(long paymentId, string agency, string account, string digit, CancellationToken cancellationToken = default);
}
