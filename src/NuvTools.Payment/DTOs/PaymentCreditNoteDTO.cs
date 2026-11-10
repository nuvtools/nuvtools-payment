namespace NuvTools.Payment.DTOs;

/// <summary>A credit note: part or all of a paid invoice given back, as its own document.</summary>
/// <param name="CreditNoteId">The provider's identifier for it.</param>
/// <param name="InvoiceId">The invoice it credits.</param>
/// <param name="Number">The number printed on it.</param>
/// <param name="CurrencyCode">ISO 4217, upper case.</param>
/// <param name="AmountMinor">How much it credits, in the currency's minor units.</param>
/// <param name="IsVoid">Whether it was voided after it was issued.</param>
/// <param name="Memo">The reason given, as printed on it.</param>
/// <param name="CreatedUtc">When it was issued.</param>
/// <param name="Metadata">What the caller stored on it.</param>
public record PaymentCreditNoteDTO(
    string CreditNoteId,
    string InvoiceId,
    string? Number,
    string CurrencyCode,
    long AmountMinor,
    bool IsVoid,
    string? Memo,
    DateTimeOffset CreatedUtc,
    IReadOnlyDictionary<string, string> Metadata);
