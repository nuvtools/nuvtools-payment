using NuvTools.Payment.Enumerations;

namespace NuvTools.Payment.DTOs;

/// <summary>
/// A webhook whose signature has been verified, read into the few fields a caller acts on.
/// </summary>
/// <remarks>
/// <para>
/// The provider client does the reading, deliberately: which field of which payload holds the
/// customer is the provider's business, and a caller that dug it out of
/// <paramref name="PayloadJson"/> itself would be coupled to a shape it does not control.
/// </para>
/// <para>
/// <paramref name="PayloadJson"/> is still carried, for a caller that needs something this record
/// does not model. Treat it as the provider's, not as a contract.
/// </para>
/// </remarks>
/// <param name="EventId">
/// The provider's own event identifier — the one to key idempotency on, because a provider that
/// delivers at least once will deliver the same event twice.
/// </param>
/// <param name="Type">What the event is, in terms a caller can act on.</param>
/// <param name="ProviderType">
/// The provider's own name for it, kept for the record a caller writes: it says what really arrived,
/// including for <see cref="PaymentEventType.Unknown"/>.
/// </param>
/// <param name="PayeeAccountId">The payee account the event is about, when it is about one.</param>
/// <param name="ObjectId">The identifier of what the event happened to — a payment, an account.</param>
/// <param name="CustomerId">The customer the event is about, when it names one.</param>
/// <param name="PaymentMethodId">The payment method the event is about, when it names one.</param>
/// <param name="FailureReason">Why the provider refused, on an event that reports a refusal.</param>
/// <param name="PayloadJson">The raw body, exactly as it was signed.</param>
public record PaymentEventDTO(
    string EventId,
    PaymentEventType Type,
    string ProviderType,
    string? PayeeAccountId,
    string? ObjectId,
    string? CustomerId,
    string? PaymentMethodId,
    string? FailureReason,
    string PayloadJson);
