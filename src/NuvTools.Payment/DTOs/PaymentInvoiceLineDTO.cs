namespace NuvTools.Payment.DTOs;

/// <summary>One line of an invoice as the customer reads it.</summary>
/// <param name="Description">What was charged for.</param>
/// <param name="Quantity">How many units, when the line was priced per unit.</param>
/// <param name="AmountMinor">The line's amount, in the currency's minor units.</param>
public record PaymentInvoiceLineDTO(string? Description, long? Quantity, long AmountMinor);
