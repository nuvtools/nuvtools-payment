namespace NuvTools.Payment.DTOs.Requests;

/// <summary>A provider-hosted page that starts a subscription.</summary>
/// <param name="CustomerId">Who pays. The customer must already exist at the provider.</param>
/// <param name="PriceLookupKey">
/// The lookup key of the price to subscribe to. A key rather than a price identifier, so a price can be
/// replaced at the provider by moving the key without the caller changing anything.
/// </param>
/// <param name="SuccessUrl">Where the customer returns after paying.</param>
/// <param name="CancelUrl">Where the customer returns if they give up.</param>
/// <param name="ClientReferenceId">The caller's own identifier for what is being bought, echoed on the completed checkout.</param>
/// <param name="Metadata">Stored on the subscription the checkout creates; the caller's own identifiers belong here.</param>
/// <param name="IdempotencyKey">Makes a retried request return the same page rather than open a second one.</param>
public record SubscriptionCheckoutRequest(
    string CustomerId,
    string PriceLookupKey,
    string SuccessUrl,
    string CancelUrl,
    string? ClientReferenceId = null,
    IDictionary<string, string>? Metadata = null,
    string? IdempotencyKey = null);
