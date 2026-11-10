namespace NuvTools.Payment.DTOs.Requests;

/// <summary>Part or all of a paid invoice given back to the customer, with the money refunded.</summary>
/// <param name="InvoiceId">The invoice being credited.</param>
/// <param name="AmountMinor">How much, in the currency's minor units; at most what was paid.</param>
/// <param name="IdempotencyKey">
/// The caller's own key. A customer refunded twice is money gone, so it must never be reused across two
/// different credit notes.
/// </param>
/// <param name="Memo">Why, printed on the credit note.</param>
/// <param name="Metadata">Stored on the credit note; the caller's own identifiers belong here.</param>
public record PaymentCreditNoteRequest(
    string InvoiceId,
    long AmountMinor,
    string IdempotencyKey,
    string? Memo = null,
    IDictionary<string, string>? Metadata = null);
