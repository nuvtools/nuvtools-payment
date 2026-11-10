using NuvTools.Payment.Enumerations;

namespace NuvTools.Payment.DTOs;

/// <summary>An invoice as the provider holds it now.</summary>
/// <remarks>
/// The document itself — its number, its PDF, its hosted page — is the provider's. A caller keeps a copy
/// of what it shows in a list and refreshes it when an event about the invoice arrives.
/// </remarks>
/// <param name="InvoiceId">The provider's identifier for it.</param>
/// <param name="CustomerId">Who owes it.</param>
/// <param name="Number">The number printed on it; <see langword="null"/> while it is a draft.</param>
/// <param name="Status">Where it stands.</param>
/// <param name="ProviderStatus">The provider's own name for the status, kept for <see cref="PaymentInvoiceStatusType.Unknown"/>.</param>
/// <param name="CurrencyCode">ISO 4217, upper case.</param>
/// <param name="TotalMinor">What it charges, in the currency's minor units.</param>
/// <param name="AmountPaidMinor">What has been paid of it.</param>
/// <param name="AmountDueMinor">What is still owed.</param>
/// <param name="CreatedUtc">When it was created.</param>
/// <param name="PeriodStartUtc">The start of the period it bills, when it bills one.</param>
/// <param name="PeriodEndUtc">The end of the period it bills, when it bills one.</param>
/// <param name="PaidUtc">When it was paid.</param>
/// <param name="HostedUrl">The provider's page for it, where it can be paid and read.</param>
/// <param name="SubscriptionId">The subscription it bills, when it bills one.</param>
/// <param name="PaymentIntentId">The payment that pays it, once one was attempted.</param>
/// <param name="ChargeId">The charge that took the money, once it was taken — what a transfer funded by it names.</param>
/// <param name="FailureReason">Why its last charge was refused, while it is open after a refusal.</param>
/// <param name="Metadata">What the caller stored on it.</param>
/// <param name="Lines">What it charges for.</param>
public record PaymentInvoiceDTO(
    string InvoiceId,
    string CustomerId,
    string? Number,
    PaymentInvoiceStatusType Status,
    string ProviderStatus,
    string CurrencyCode,
    long TotalMinor,
    long AmountPaidMinor,
    long AmountDueMinor,
    DateTimeOffset CreatedUtc,
    DateTimeOffset? PeriodStartUtc,
    DateTimeOffset? PeriodEndUtc,
    DateTimeOffset? PaidUtc,
    string? HostedUrl,
    string? SubscriptionId,
    string? PaymentIntentId,
    string? ChargeId,
    string? FailureReason,
    IReadOnlyDictionary<string, string> Metadata,
    IReadOnlyList<PaymentInvoiceLineDTO> Lines);
