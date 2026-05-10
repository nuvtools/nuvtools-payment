using NuvTools.Common.ResultWrapper;
using NuvTools.Payment.Models.BatchPayment;
using NuvTools.Payment.Models.Common;

namespace NuvTools.Payment.Contracts;

/// <summary>
/// Provider-neutral contract for batch bank slip payment — the payer-side role
/// where a company pays multiple slips to suppliers. Implemented by Banco do Brasil.
/// </summary>
public interface IBankSlipBatchPaymentClient
{
    Task<IResult<AccessToken>> AuthenticateAsync(string scope, CancellationToken cancellationToken = default);

    Task<IResult<BankSlipBatchPaymentResult>> CreateBatchAsync(BankSlipBatchPaymentRequest request, CancellationToken cancellationToken = default);

    Task<IResult<BankSlipBatchPaymentStatus>> GetBatchAsync(BankSlipBatchPaymentReference reference, CancellationToken cancellationToken = default);
}
