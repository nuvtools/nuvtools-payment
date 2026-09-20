using NuvTools.Payment.Enumerations;

namespace NuvTools.Payment.Stripe.ApiClient.Services;

/// <summary>
/// Stripe's refund statuses, in the outcomes a caller acts on.
/// </summary>
/// <remarks>
/// A refund has no <c>requires_action</c>: nobody has to be present to be given money back. The
/// interesting one is <c>pending</c>, which is the normal answer for anything that is not a card —
/// the money is on its way, and the webhook says when it lands.
/// </remarks>
internal static class StripeRefundStatus
{
    public static PaymentStatusType From(string? status) => status switch
    {
        "succeeded" => PaymentStatusType.Succeeded,
        "failed" or "canceled" => PaymentStatusType.Failed,
        _ => PaymentStatusType.Pending
    };
}
