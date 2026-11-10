using NuvTools.Payment.Enumerations;

namespace NuvTools.Payment.DTOs;

/// <summary>A subscription just started from a saved payment method, and what became of its first payment.</summary>
/// <param name="Subscription">The subscription as it stands now.</param>
/// <param name="FirstPayment">
/// The first invoice's payment: <see cref="PaymentStatusType.Succeeded"/> when it was charged (or there
/// was nothing to charge), <see cref="PaymentStatusType.RequiresAction"/> when the card wants its owner
/// to confirm, and <see cref="PaymentStatusType.Failed"/> when it was refused.
/// </param>
/// <param name="ClientSecret">
/// What the browser confirms the first payment with, on <see cref="PaymentStatusType.RequiresAction"/>.
/// A credential scoped to that payment: it goes back to the customer who started it and nowhere else.
/// </param>
/// <param name="FailureReason">Why the first payment was refused, on <see cref="PaymentStatusType.Failed"/>.</param>
public record PaymentSubscriptionStartDTO(
    PaymentSubscriptionDTO Subscription,
    PaymentStatusType FirstPayment,
    string? ClientSecret,
    string? FailureReason);
