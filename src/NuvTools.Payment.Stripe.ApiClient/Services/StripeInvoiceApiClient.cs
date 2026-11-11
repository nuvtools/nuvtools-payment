using Microsoft.Extensions.Logging;
using NuvTools.Common.ResultWrapper;
using NuvTools.Payment.Contracts;
using NuvTools.Payment.DTOs;
using NuvTools.Payment.DTOs.Requests;
using Stripe;
using System.Globalization;

namespace NuvTools.Payment.Stripe.ApiClient.Services;

/// <inheritdoc cref="IPaymentInvoiceClient" />
/// <remarks>
/// <para>
/// Stripe Invoicing. An invoice the caller issues is built in three keyed steps — the draft, one item per
/// line, the finalization — and then paid once. Each step's idempotency key is derived from the caller's,
/// so a request repeated after a failure part-way picks up where it stopped rather than issuing twice.
/// </para>
/// <para>
/// The PDFs are fetched here, server side. Stripe's link to them needs no credential, so handing it to a
/// browser would hand the document to whoever the link reaches.
/// </para>
/// </remarks>
public class StripeInvoiceApiClient(
    IStripeClient client,
    IHttpClientFactory httpClientFactory,
    ILogger<StripeInvoiceApiClient> logger) : IPaymentInvoiceClient
{
    private const string PdfContentType = "application/pdf";

    /// <summary>The payment that paid an invoice, and the charge under it, three levels down.</summary>
    private static readonly List<string> PaymentExpansion = ["payments.data.payment.payment_intent"];

    /// <summary>Metadata keys the period travels in, since a one-off invoice has no period of its own.</summary>
    internal const string PeriodStartKey = "nuv_period_start";
    internal const string PeriodEndKey = "nuv_period_end";

    private readonly InvoiceService _invoices = new(client);
    private readonly InvoiceItemService _invoiceItems = new(client);
    private readonly CreditNoteService _creditNotes = new(client);

    public Task<IResult<PaymentInvoiceDTO>> CreateInvoiceAsync(
        PaymentInvoiceRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.IdempotencyKey);

        if (request.Lines is not { Count: > 0 })
            throw new ArgumentException("An invoice needs at least one line.", nameof(request));

        return StripeCall.RunAsync(logger, nameof(CreateInvoiceAsync), async () =>
        {
            var currency = request.CurrencyCode.Trim().ToLowerInvariant();
            var metadata = request.Metadata?.ToDictionary(e => e.Key, e => e.Value) ?? [];

            if (request.PeriodStartUtc is { } start) metadata[PeriodStartKey] = start.UtcDateTime.ToString("O", CultureInfo.InvariantCulture);
            if (request.PeriodEndUtc is { } end) metadata[PeriodEndKey] = end.UtcDateTime.ToString("O", CultureInfo.InvariantCulture);

            var invoice = await _invoices.CreateAsync(new InvoiceCreateOptions
            {
                Customer = request.CustomerId,
                Currency = currency,
                CollectionMethod = request.ChargeAutomatically ? "charge_automatically" : "send_invoice",
                DaysUntilDue = request.ChargeAutomatically ? null : 7,
                AutoAdvance = true,

                // Only the lines below: an item left pending on the customer by somebody else is not
                // this invoice's to collect.
                PendingInvoiceItemsBehavior = "exclude",
                Description = request.Description,
                StatementDescriptor = request.StatementDescriptor,
                Metadata = metadata
            }, Keyed(request.IdempotencyKey, "invoice"), cancellationToken);

            for (var i = 0; i < request.Lines.Count; i++)
            {
                var line = request.Lines[i];

                await _invoiceItems.CreateAsync(new InvoiceItemCreateOptions
                {
                    Customer = request.CustomerId,
                    Invoice = invoice.Id,
                    Currency = currency,
                    Amount = line.AmountMinor,
                    Description = line.Description,
                    Metadata = line.Metadata?.ToDictionary(e => e.Key, e => e.Value)
                }, Keyed(request.IdempotencyKey, $"line-{i}"), cancellationToken);
            }

            invoice = await _invoices.FinalizeInvoiceAsync(invoice.Id,
                new InvoiceFinalizeOptions { AutoAdvance = true },
                Keyed(request.IdempotencyKey, "finalize"), cancellationToken);

            if (request.ChargeAutomatically && invoice.Status == "open" && invoice.AmountDue > 0)
                await TryPayAsync(invoice.Id, Keyed(request.IdempotencyKey, "pay"), cancellationToken);

            return Describe(await ReadAsync(invoice.Id, cancellationToken));
        });
    }

    public Task<IResult<PaymentInvoiceDTO>> GetInvoiceAsync(string invoiceId, CancellationToken cancellationToken = default) =>
        StripeCall.RunAsync(logger, nameof(GetInvoiceAsync), async () =>
            Describe(await ReadAsync(invoiceId, cancellationToken)));

    public Task<IResult<PaymentInvoicePageDTO>> ListInvoicesAsync(
        string customerId, string? startingAfter = null, int limit = 20, CancellationToken cancellationToken = default) =>
        StripeCall.RunAsync(logger, nameof(ListInvoicesAsync), async () =>
        {
            var page = await _invoices.ListAsync(new InvoiceListOptions
            {
                Customer = customerId,
                StartingAfter = string.IsNullOrWhiteSpace(startingAfter) ? null : startingAfter,
                Limit = Math.Clamp(limit, 1, 100)
            }, cancellationToken: cancellationToken);

            // A draft is still being written and owed by nobody; a list of what was billed leaves it out.
            return new PaymentInvoicePageDTO(
                [.. page.Data.Where(e => e.Status != "draft").Select(Describe)],
                page.HasMore);
        });

    public async Task<IResult<PaymentDocumentDTO>> GetInvoicePdfAsync(string invoiceId, CancellationToken cancellationToken = default)
    {
        var read = await StripeCall.RunAsync(logger, nameof(GetInvoicePdfAsync), () =>
            _invoices.GetAsync(invoiceId, cancellationToken: cancellationToken));

        if (!read.Succeeded || read.Data is not { } invoice) return Result<PaymentDocumentDTO>.Fail(read.Messages);

        if (string.IsNullOrWhiteSpace(invoice.InvoicePdf))
            return Result<PaymentDocumentDTO>.FailNotFound($"Invoice {invoiceId} has no PDF yet: it is still a draft.");

        return await DownloadAsync(invoice.InvoicePdf, $"{invoice.Number ?? invoice.Id}.pdf", cancellationToken);
    }

    public async Task<IResult<PaymentInvoiceDTO>> PayInvoiceAsync(string invoiceId, CancellationToken cancellationToken = default)
    {
        var paid = await StripeCall.RunAsync(logger, nameof(PayInvoiceAsync), () =>
            _invoices.PayAsync(invoiceId, new InvoicePayOptions { OffSession = true }, cancellationToken: cancellationToken));

        // A refusal is the answer the customer needs to read, so it is the result rather than the
        // invoice as it stands.
        if (!paid.Succeeded) return Result<PaymentInvoiceDTO>.Fail(paid.Messages);

        return await GetInvoiceAsync(invoiceId, cancellationToken);
    }

    public Task<IResult<PaymentCreditNoteDTO>> CreateCreditNoteAsync(
        PaymentCreditNoteRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.IdempotencyKey);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(request.AmountMinor);

        return StripeCall.RunAsync(logger, nameof(CreateCreditNoteAsync), async () =>
        {
            var note = await _creditNotes.CreateAsync(new CreditNoteCreateOptions
            {
                Invoice = request.InvoiceId,
                Amount = request.AmountMinor,

                // The money goes back to the card it came from, not to a balance the customer may
                // never use.
                RefundAmount = request.AmountMinor,
                Memo = request.Memo,
                Metadata = request.Metadata?.ToDictionary(e => e.Key, e => e.Value)
            }, new RequestOptions { IdempotencyKey = request.IdempotencyKey }, cancellationToken);

            return Describe(note);
        });
    }

    public Task<IResult<PaymentCreditNoteDTO>> GetCreditNoteAsync(string creditNoteId, CancellationToken cancellationToken = default) =>
        StripeCall.RunAsync(logger, nameof(GetCreditNoteAsync), async () =>
            Describe(await _creditNotes.GetAsync(creditNoteId, cancellationToken: cancellationToken)));

    public async Task<IResult<PaymentDocumentDTO>> GetCreditNotePdfAsync(string creditNoteId, CancellationToken cancellationToken = default)
    {
        var read = await StripeCall.RunAsync(logger, nameof(GetCreditNotePdfAsync), () =>
            _creditNotes.GetAsync(creditNoteId, cancellationToken: cancellationToken));

        if (!read.Succeeded || read.Data is not { } note) return Result<PaymentDocumentDTO>.Fail(read.Messages);

        if (string.IsNullOrWhiteSpace(note.Pdf))
            return Result<PaymentDocumentDTO>.FailNotFound($"Credit note {creditNoteId} has no PDF.");

        return await DownloadAsync(note.Pdf, $"{note.Number ?? note.Id}.pdf", cancellationToken);
    }

    private Task<Invoice> ReadAsync(string invoiceId, CancellationToken cancellationToken) =>
        _invoices.GetAsync(invoiceId, new InvoiceGetOptions { Expand = PaymentExpansion }, cancellationToken: cancellationToken);

    /// <summary>
    /// The first charge, attempted now rather than left to Stripe's own schedule. A refusal is not a
    /// failure of the call: the invoice stays open, the outcome arrives by webhook, and the customer pays
    /// it again after changing the card.
    /// </summary>
    private async Task TryPayAsync(string invoiceId, RequestOptions options, CancellationToken cancellationToken)
    {
        try
        {
            await _invoices.PayAsync(invoiceId, new InvoicePayOptions { OffSession = true }, options, cancellationToken);
        }
        catch (StripeException exception)
        {
            // A declined card, and a customer with no card at all, alike: the invoice is issued either
            // way, and failing here would fail every repeat too, since Stripe replays this key's answer.
            logger.LogInformation("Stripe refused the first charge of invoice {InvoiceId}: {Code}.", invoiceId, exception.StripeError?.Code);
        }
    }

    private async Task<IResult<PaymentDocumentDTO>> DownloadAsync(string url, string fileName, CancellationToken cancellationToken)
    {
        try
        {
            using var http = httpClientFactory.CreateClient(DependencyInjection.HttpClientName);
            var content = await http.GetByteArrayAsync(url, cancellationToken);

            return Result<PaymentDocumentDTO>.Success(new PaymentDocumentDTO(fileName, PdfContentType, content), string.Empty);
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Stripe's document {FileName} could not be fetched.", fileName);

            return Result<PaymentDocumentDTO>.Fail("The document could not be fetched from Stripe.");
        }
    }

    private static RequestOptions Keyed(string key, string step) => new() { IdempotencyKey = $"{key}:{step}" };

    private static PaymentInvoiceDTO Describe(Invoice invoice)
    {
        var intent = invoice.Payments?.Data?.Select(e => e.Payment?.PaymentIntent).FirstOrDefault(e => e is not null);
        var metadata = invoice.Metadata ?? new Dictionary<string, string>();

        return new PaymentInvoiceDTO(
            invoice.Id,
            invoice.CustomerId,
            invoice.Number,
            StripeInvoiceStatus.From(invoice.Status),
            invoice.Status ?? string.Empty,
            (invoice.Currency ?? string.Empty).ToUpperInvariant(),
            invoice.Total,
            invoice.AmountPaid,
            invoice.AmountRemaining,
            Utc(invoice.Created),
            Period(metadata, PeriodStartKey),
            Period(metadata, PeriodEndKey),
            invoice.StatusTransitions?.PaidAt is { } paidAt ? Utc(paidAt) : null,
            invoice.HostedInvoiceUrl,
            invoice.Parent?.SubscriptionDetails?.SubscriptionId,
            intent?.Id ?? invoice.Payments?.Data?.Select(e => e.Payment?.PaymentIntentId).FirstOrDefault(e => e is not null),
            intent?.LatestChargeId,
            invoice.Status == "open" ? intent?.LastPaymentError?.Message : null,
            metadata,
            [.. invoice.Lines?.Data?.Select(e => new PaymentInvoiceLineDTO(e.Description, e.Quantity, e.Amount)) ?? []]);
    }

    private static PaymentCreditNoteDTO Describe(CreditNote note) =>
        new(note.Id,
            note.InvoiceId,
            note.Number,
            (note.Currency ?? string.Empty).ToUpperInvariant(),
            note.Total,
            note.Status == "void",
            note.Memo,
            Utc(note.Created),
            note.Metadata ?? new Dictionary<string, string>());

    private static DateTimeOffset Utc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private static DateTimeOffset? Period(IDictionary<string, string> metadata, string key) =>
        metadata.TryGetValue(key, out var value)
        && DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
            ? parsed
            : null;
}
