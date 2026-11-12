namespace NuvTools.Payment.DTOs.Requests;

/// <summary>An invoice the caller issues for what it already measured — usage, typically — and has the provider charge.</summary>
/// <param name="CustomerId">Who owes it.</param>
/// <param name="CurrencyCode">ISO 4217, in any case.</param>
/// <param name="Lines">What it charges for; at least one.</param>
/// <param name="IdempotencyKey">
/// The caller's own key, from which every provider call is keyed. The same key issues the same invoice
/// once, however often the request is repeated.
/// </param>
/// <param name="Description">A memo printed on the invoice.</param>
/// <param name="StatementDescriptor">
/// What the customer's card statement shows for the charge, naming the product the money is for. The
/// account's own descriptor when <see langword="null"/>.
/// </param>
/// <param name="PeriodStartUtc">The start of the period the lines cover, printed on each.</param>
/// <param name="PeriodEndUtc">The end of the period the lines cover.</param>
/// <param name="Metadata">Stored on the invoice; the caller's own identifiers belong here.</param>
/// <param name="ChargeAutomatically">
/// Whether the provider charges the customer's default payment method as soon as it is issued. When
/// <see langword="false"/> it stays open for the customer to pay.
/// </param>
/// <param name="PaymentMethodId">
/// The customer's saved payment method this invoice is charged to, instead of the customer's default —
/// for a customer who pays each product with a different card. It must already be attached to the
/// customer. The default when <see langword="null"/>.
/// </param>
public record PaymentInvoiceRequest(
    string CustomerId,
    string CurrencyCode,
    IReadOnlyList<PaymentInvoiceLineRequest> Lines,
    string IdempotencyKey,
    string? Description = null,
    string? StatementDescriptor = null,
    DateTimeOffset? PeriodStartUtc = null,
    DateTimeOffset? PeriodEndUtc = null,
    IDictionary<string, string>? Metadata = null,
    bool ChargeAutomatically = true,
    string? PaymentMethodId = null);
