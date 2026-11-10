using NuvTools.Common.ResultWrapper;
using NuvTools.Payment.DTOs;
using NuvTools.Payment.DTOs.Requests;

namespace NuvTools.Payment.Contracts;

/// <summary>
/// Invoices kept by the provider: the ones its subscriptions issue, and the ones the caller issues for
/// what it measured itself. Listing them, serving their PDF, paying one again, and crediting one back.
/// </summary>
/// <remarks>
/// The provider numbers, renders, charges and retries the invoice. A caller keeps a copy of what it
/// lists and refreshes it from <see cref="GetInvoiceAsync"/> when an event about it arrives.
/// </remarks>
public interface IPaymentInvoiceClient
{
    /// <summary>
    /// Issues an invoice with the caller's lines and, unless asked otherwise, has the provider charge the
    /// customer's default payment method for it straight away.
    /// </summary>
    /// <remarks>
    /// The charge's outcome arrives by webhook. The invoice returned is as it stood once issued — open,
    /// usually, with its first charge on its way.
    /// </remarks>
    /// <param name="request">Who owes it, for what, and the key that makes a repeat harmless.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    Task<IResult<PaymentInvoiceDTO>> CreateInvoiceAsync(
        PaymentInvoiceRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>An invoice as the provider holds it now.</summary>
    /// <param name="invoiceId">The provider's identifier for it.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    Task<IResult<PaymentInvoiceDTO>> GetInvoiceAsync(
        string invoiceId,
        CancellationToken cancellationToken = default);

    /// <summary>A customer's invoices, newest first, drafts left out.</summary>
    /// <param name="customerId">Whose invoices.</param>
    /// <param name="startingAfter">The last invoice of the previous page, to read the next.</param>
    /// <param name="limit">How many, 1 to 100.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    Task<IResult<PaymentInvoicePageDTO>> ListInvoicesAsync(
        string customerId,
        string? startingAfter = null,
        int limit = 20,
        CancellationToken cancellationToken = default);

    /// <summary>The invoice's PDF, fetched by the server so the provider's link never reaches a browser.</summary>
    /// <param name="invoiceId">The provider's identifier for it.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    Task<IResult<PaymentDocumentDTO>> GetInvoicePdfAsync(
        string invoiceId,
        CancellationToken cancellationToken = default);

    /// <summary>Charges an open invoice again, from the customer's default payment method now.</summary>
    /// <remarks>For after the customer replaced a card that was refused.</remarks>
    /// <param name="invoiceId">The provider's identifier for it.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    Task<IResult<PaymentInvoiceDTO>> PayInvoiceAsync(
        string invoiceId,
        CancellationToken cancellationToken = default);

    /// <summary>Gives back part or all of a paid invoice: a credit note, with the money refunded to the card.</summary>
    /// <param name="request">Which invoice, how much, and the key that makes a repeat harmless.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    Task<IResult<PaymentCreditNoteDTO>> CreateCreditNoteAsync(
        PaymentCreditNoteRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>A credit note as the provider holds it now.</summary>
    /// <param name="creditNoteId">The provider's identifier for it.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    Task<IResult<PaymentCreditNoteDTO>> GetCreditNoteAsync(
        string creditNoteId,
        CancellationToken cancellationToken = default);

    /// <summary>The credit note's PDF, fetched by the server.</summary>
    /// <param name="creditNoteId">The provider's identifier for it.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    Task<IResult<PaymentDocumentDTO>> GetCreditNotePdfAsync(
        string creditNoteId,
        CancellationToken cancellationToken = default);
}
