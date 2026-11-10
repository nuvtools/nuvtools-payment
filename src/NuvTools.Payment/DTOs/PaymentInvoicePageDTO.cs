namespace NuvTools.Payment.DTOs;

/// <summary>One page of a customer's invoices, newest first.</summary>
/// <param name="Invoices">The invoices on this page.</param>
/// <param name="HasMore">Whether older ones follow; ask again from the last one's identifier.</param>
public record PaymentInvoicePageDTO(IReadOnlyList<PaymentInvoiceDTO> Invoices, bool HasMore);
