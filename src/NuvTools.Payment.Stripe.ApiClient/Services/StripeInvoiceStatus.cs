using NuvTools.Payment.Enumerations;

namespace NuvTools.Payment.Stripe.ApiClient.Services;

/// <summary>Stripe's invoice statuses, in the package's neutral terms.</summary>
internal static class StripeInvoiceStatus
{
    public static PaymentInvoiceStatusType From(string? status) => status switch
    {
        "draft" => PaymentInvoiceStatusType.Draft,
        "open" => PaymentInvoiceStatusType.Open,
        "paid" => PaymentInvoiceStatusType.Paid,
        "void" => PaymentInvoiceStatusType.Void,
        "uncollectible" => PaymentInvoiceStatusType.Uncollectible,
        _ => PaymentInvoiceStatusType.Unknown
    };
}
