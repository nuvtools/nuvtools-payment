namespace NuvTools.Payment.DTOs;

/// <summary>
/// What a browser needs to save a payment method without a payment, for a caller that hosts its own
/// form.
/// </summary>
/// <param name="IntentId">The provider's identifier for the attempt.</param>
/// <param name="ClientSecret">
/// What the browser confirms with. It is scoped to this one attempt, but it is still a credential:
/// it belongs in the response to the customer it was created for, and nowhere else.
/// </param>
public record PaymentMethodIntentDTO(string IntentId, string? ClientSecret);
