namespace NuvTools.Payment.DTOs.Requests;

/// <summary>One line of an invoice the caller issues.</summary>
/// <param name="Description">What is charged for, as the customer will read it.</param>
/// <param name="AmountMinor">The line's whole amount, in the currency's minor units.</param>
/// <param name="Metadata">Carried on the provider's line; the caller's own identifiers belong here.</param>
public record PaymentInvoiceLineRequest(
    string Description,
    long AmountMinor,
    IDictionary<string, string>? Metadata = null);
