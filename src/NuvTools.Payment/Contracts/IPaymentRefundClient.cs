using NuvTools.Common.ResultWrapper;
using NuvTools.Payment.DTOs;
using NuvTools.Payment.DTOs.Requests;

namespace NuvTools.Payment.Contracts;

/// <summary>
/// Giving money back: refunding part or all of a payment the customer already made.
/// </summary>
/// <remarks>
/// <para>
/// <b>Separate from <see cref="IPaymentChargeClient"/>, for the reason charges and transfers are
/// separate.</b> Refunding is not the inverse of charging: it names a payment rather than a
/// customer, it can be partial and repeated until the payment is exhausted, and a caller that can
/// charge is not automatically a caller that may give money back. Keeping it its own contract lets a
/// provider that cannot refund simply not implement it, and lets a composition root grant the two
/// capabilities separately.
/// </para>
/// <para>
/// <b>What it does not do is claw back a payee.</b> Money already transferred to a payee is the
/// caller's problem to reconcile — by netting it off what the payee is owed next, which is a
/// decision only the caller can make. A provider API that reverses a transfer exists, but using it
/// would take money out of an account the payee may have already spent from.
/// </para>
/// </remarks>
public interface IPaymentRefundClient
{
    /// <summary>Refunds a payment, in whole or in part.</summary>
    /// <param name="request">Which payment, how much, and the key that makes a retry safe.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    Task<IResult<PaymentRefundDTO>> RefundAsync(
        PaymentRefundRequest request,
        CancellationToken cancellationToken = default);
}
