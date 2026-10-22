using NuvTools.Payment.Enumerations;

namespace NuvTools.Payment.Stripe.ApiClient.Services;

/// <summary>Stripe's subscription statuses, in the package's neutral terms.</summary>
internal static class StripeSubscriptionStatus
{
    public static PaymentSubscriptionStatusType From(string? status) => status switch
    {
        "incomplete" => PaymentSubscriptionStatusType.Incomplete,
        "incomplete_expired" => PaymentSubscriptionStatusType.IncompleteExpired,
        "trialing" => PaymentSubscriptionStatusType.Trialing,
        "active" => PaymentSubscriptionStatusType.Active,
        "past_due" => PaymentSubscriptionStatusType.PastDue,
        "canceled" => PaymentSubscriptionStatusType.Canceled,
        "unpaid" => PaymentSubscriptionStatusType.Unpaid,
        "paused" => PaymentSubscriptionStatusType.Paused,
        _ => PaymentSubscriptionStatusType.Unknown
    };
}
