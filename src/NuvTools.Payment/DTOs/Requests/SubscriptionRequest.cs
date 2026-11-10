namespace NuvTools.Payment.DTOs.Requests;

/// <summary>A subscription started by the caller, charged to a payment method the customer already saved.</summary>
/// <param name="CustomerId">Who pays.</param>
/// <param name="PriceLookupKey">The lookup key of the price to subscribe to.</param>
/// <param name="PaymentMethodId">The saved method to charge; the customer's default when <see langword="null"/>.</param>
/// <param name="Metadata">Stored on the subscription; the caller's own identifiers belong here.</param>
/// <param name="IdempotencyKey">Makes a retried request start one subscription, not two.</param>
public record SubscriptionRequest(
    string CustomerId,
    string PriceLookupKey,
    string? PaymentMethodId = null,
    IDictionary<string, string>? Metadata = null,
    string? IdempotencyKey = null);
