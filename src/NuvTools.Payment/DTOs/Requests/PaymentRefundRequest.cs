namespace NuvTools.Payment.DTOs.Requests;

/// <summary>What one refund needs.</summary>
/// <param name="PaymentId">
/// The payment being given back, as the provider named it when it was taken — not the customer. A
/// refund belongs to one payment, because that is what the money can travel back along.
/// </param>
/// <param name="AmountMinor">
/// How much, in the currency's minor units. Always stated rather than defaulted to the whole
/// payment: a partial refund is the common case for a marketplace crediting one line of an invoice,
/// and a null that quietly meant "all of it" is the kind of default that refunds a month when
/// somebody meant to refund a day.
/// </param>
/// <param name="Reason">Why, for the provider's own record. Free text; providers vary.</param>
/// <param name="IdempotencyKey">
/// The caller's own key. Refunding twice gives away the money twice and cannot be taken back, so
/// this is as load-bearing here as it is on a transfer.
/// </param>
/// <param name="Metadata">Carried on the provider's object; the caller's own identifiers belong here.</param>
public record PaymentRefundRequest(
    string PaymentId,
    long AmountMinor,
    string? Reason,
    string IdempotencyKey,
    IDictionary<string, string>? Metadata = null);
