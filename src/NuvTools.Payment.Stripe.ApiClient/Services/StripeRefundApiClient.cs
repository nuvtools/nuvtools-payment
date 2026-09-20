using Microsoft.Extensions.Logging;
using NuvTools.Common.ResultWrapper;
using NuvTools.Payment.Contracts;
using NuvTools.Payment.DTOs;
using NuvTools.Payment.DTOs.Requests;
using Stripe;

namespace NuvTools.Payment.Stripe.ApiClient.Services;

/// <inheritdoc cref="IPaymentRefundClient" />
/// <remarks>
/// <para>
/// Refunds the payment intent, which is what the charge client returned and the caller stored.
/// Stripe refunds against a payment intent or a charge; the intent is the identifier a caller
/// already holds, so asking it for the charge first would be a round trip for nothing.
/// </para>
/// <para>
/// <b><c>ReverseTransfer</c> is deliberately not set.</b> Stripe can pull the money back out of the
/// connected account in the same call, and this client never does: the payee may already have been
/// paid out to their bank, and a reversal that overdraws them is a worse problem than a balance the
/// platform nets off what it owes them next. Who absorbs a refund is the caller's decision, and it
/// belongs in the caller's own records rather than in a flag here.
/// </para>
/// </remarks>
public class StripeRefundApiClient(IStripeClient client, ILogger<StripeRefundApiClient> logger) : IPaymentRefundClient
{
    private readonly RefundService _refunds = new(client);

    public Task<IResult<PaymentRefundDTO>> RefundAsync(
        PaymentRefundRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return StripeCall.RunAsync(logger, nameof(RefundAsync), async () =>
        {
            var refund = await _refunds.CreateAsync(new RefundCreateOptions
            {
                PaymentIntent = request.PaymentId,
                Amount = request.AmountMinor,
                Metadata = Metadata(request),

                // Stripe accepts only three reasons and rejects anything else, so the caller's own
                // words travel in metadata and the enumeration is left unset. Guessing at it would
                // also risk marking the charge as fraudulent, which has consequences for the account
                // that no caller asked for.
                Reason = null
            },
            new RequestOptions { IdempotencyKey = request.IdempotencyKey },
            cancellationToken);

            return new PaymentRefundDTO(
                refund.Id,
                refund.Amount,
                refund.Currency.ToUpperInvariant(),
                StripeRefundStatus.From(refund.Status));
        });
    }

    private static Dictionary<string, string>? Metadata(PaymentRefundRequest request)
    {
        var metadata = request.Metadata?.ToDictionary(e => e.Key, e => e.Value);

        if (string.IsNullOrWhiteSpace(request.Reason)) return metadata;

        metadata ??= [];
        metadata["reason"] = request.Reason;

        return metadata;
    }
}
