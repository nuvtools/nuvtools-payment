using NuvTools.Payment.Enumerations;

namespace NuvTools.Payment.Stripe.ApiClient.Services;

/// <summary>
/// Stripe's payment intent statuses, in the four outcomes a caller acts on.
/// </summary>
/// <remarks>
/// Stripe names eight states where a caller has four decisions, and the mapping is this package's
/// business rather than the caller's. The one that matters most is <c>requires_action</c>: it is not
/// a refusal, and charging again over it would charge twice.
/// </remarks>
internal static class StripePaymentStatus
{
    public static PaymentStatusType From(string? status) => status switch
    {
        "succeeded" => PaymentStatusType.Succeeded,
        "requires_action" or "requires_confirmation" or "requires_capture" => PaymentStatusType.RequiresAction,
        "requires_payment_method" or "canceled" => PaymentStatusType.Failed,
        _ => PaymentStatusType.Pending
    };
}
