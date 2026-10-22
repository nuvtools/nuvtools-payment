using NuvTools.Payment.Enumerations;

namespace NuvTools.Payment.DTOs;

/// <summary>A subscription as the provider holds it now.</summary>
/// <param name="SubscriptionId">The provider's identifier for it.</param>
/// <param name="CustomerId">Who pays for it.</param>
/// <param name="Status">Where it stands.</param>
/// <param name="ProviderStatus">The provider's own name for the status, kept for <see cref="PaymentSubscriptionStatusType.Unknown"/>.</param>
/// <param name="PriceLookupKey">
/// The lookup key of the price it is charged at — the stable name a caller's catalogue refers to —
/// or <see langword="null"/> when the price has none.
/// </param>
/// <param name="CurrentPeriodEndUtc">When the period already paid for ends.</param>
/// <param name="CancelAtPeriodEnd">Whether it ends at <paramref name="CurrentPeriodEndUtc"/> rather than renewing.</param>
/// <param name="Metadata">What the caller stored on it when it was started.</param>
public record PaymentSubscriptionDTO(
    string SubscriptionId,
    string CustomerId,
    PaymentSubscriptionStatusType Status,
    string ProviderStatus,
    string? PriceLookupKey,
    DateTimeOffset? CurrentPeriodEndUtc,
    bool CancelAtPeriodEnd,
    IReadOnlyDictionary<string, string> Metadata);
