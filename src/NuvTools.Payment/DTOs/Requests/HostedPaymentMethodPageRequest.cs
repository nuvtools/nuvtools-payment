namespace NuvTools.Payment.DTOs.Requests;

/// <summary>What a provider-hosted page for saving a payment method needs.</summary>
/// <param name="CustomerId">Whose payment method is being saved.</param>
/// <param name="SuccessUrl">Where the provider returns the customer once they have saved one.</param>
/// <param name="CancelUrl">Where it returns them if they give up.</param>
/// <param name="CurrencyCode">
/// ISO 4217, in any case: the currency the customer will later be charged in. Nothing is charged on
/// this page, but a provider offers only the methods that can pay in that currency, and only the
/// caller knows which it is. Null when the caller does not know yet; a provider that cannot choose
/// without it answers a failure saying so.
/// </param>
public record HostedPaymentMethodPageRequest(
    string CustomerId,
    string SuccessUrl,
    string CancelUrl,
    string? CurrencyCode = null);
