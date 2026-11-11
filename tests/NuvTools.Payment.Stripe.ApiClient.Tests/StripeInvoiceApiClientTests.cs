using Microsoft.Extensions.Logging.Abstractions;
using NuvTools.Payment.DTOs.Requests;
using NuvTools.Payment.Enumerations;
using NuvTools.Payment.Stripe.ApiClient.Services;
using Stripe;
using System.Net;

namespace NuvTools.Payment.Stripe.ApiClient.Tests;

public class StripeInvoiceApiClientTests
{
    private const string OpenInvoice =
        """{"id":"in_1","object":"invoice","customer":"cus_1","status":"open","currency":"brl","total":1500,"amount_paid":0,"amount_remaining":1500,"amount_due":1500,"created":1767225600,"number":"NUV-0001","invoice_pdf":"https://pay.stripe.test/invoice/in_1/pdf","metadata":{"product":"dataprovider","nuv_period_start":"2026-10-01T00:00:00.0000000Z"},"lines":{"object":"list","data":[{"id":"il_1","object":"line_item","description":"Geography API","quantity":1,"amount":1500}],"has_more":false}}""";

    private const string PaidInvoice =
        """{"id":"in_1","object":"invoice","customer":"cus_1","status":"paid","currency":"brl","total":1500,"amount_paid":1500,"amount_remaining":0,"created":1767225600,"number":"NUV-0001","status_transitions":{"paid_at":1767229200},"payments":{"object":"list","data":[{"id":"inpay_1","object":"invoice_payment","payment":{"type":"payment_intent","payment_intent":{"id":"pi_1","object":"payment_intent","status":"succeeded","latest_charge":"ch_1"}}}],"has_more":false},"metadata":{},"lines":{"object":"list","data":[],"has_more":false}}""";

    private static (StripeInvoiceApiClient Client, StubStripeHandler Stub) Create(StubStripeHandler stub) =>
        (new StripeInvoiceApiClient(
            new StripeClient("sk_test_stub", httpClient: new SystemNetHttpClient(new HttpClient(stub), maxNetworkRetries: 0)),
            new StubHttpClientFactory(stub),
            NullLogger<StripeInvoiceApiClient>.Instance), stub);

    private static readonly PaymentInvoiceRequest Request = new(
        "cus_1", "BRL",
        [new("Geography API — 1,500 calls", 1500), new("Geography API — overage", 500)],
        "invoice:42",
        StatementDescriptor: "NUV TOOLS DATAPROV",
        PeriodStartUtc: new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero),
        Metadata: new Dictionary<string, string> { ["product"] = "dataprovider" });

    [Fact]
    public async Task Issuing_BuildsTheInvoiceInKeyedSteps_AndChargesItOnce()
    {
        var (client, stub) = Create(new StubStripeHandler()
            .Answer("/v1/invoiceitems", """{"id":"ii_1","object":"invoiceitem"}""")
            .Answer("/v1/invoices/in_1/finalize", OpenInvoice)
            .Answer("/v1/invoices/in_1/pay", PaidInvoice)
            .Answer("/v1/invoices/in_1", PaidInvoice)
            .Answer("/v1/invoices", OpenInvoice));

        var result = await client.CreateInvoiceAsync(Request);

        Assert.True(result.Succeeded);
        Assert.Equal(PaymentInvoiceStatusType.Paid, result.Data!.Status);
        Assert.Equal("ch_1", result.Data.ChargeId);
        Assert.Equal("pi_1", result.Data.PaymentIntentId);

        var draft = Uri.UnescapeDataString(stub.Requests.First(r => r.Path == "/v1/invoices").Body!);

        Assert.Contains("collection_method=charge_automatically", draft);
        Assert.Contains("pending_invoice_items_behavior=exclude", draft);
        Assert.Contains("statement_descriptor=NUV TOOLS DATAPROV", draft.Replace('+', ' '));
        Assert.Contains("metadata[nuv_period_start]=2026-10-01", draft);
        Assert.Equal(2, stub.Requests.Count(r => r.Path == "/v1/invoiceitems"));
        Assert.Single(stub.Requests, r => r.Path == "/v1/invoices/in_1/pay");
    }

    [Fact]
    public async Task ARefusedFirstCharge_LeavesTheInvoiceOpen_RatherThanFailingTheCall()
    {
        var (client, _) = Create(new StubStripeHandler()
            .Answer("/v1/invoiceitems", """{"id":"ii_1","object":"invoiceitem"}""")
            .Answer("/v1/invoices/in_1/finalize", OpenInvoice)
            .Answer("/v1/invoices/in_1/pay", """{"error":{"message":"Your card was declined.","type":"card_error","code":"card_declined"}}""", HttpStatusCode.PaymentRequired)
            .Answer("/v1/invoices/in_1", OpenInvoice)
            .Answer("/v1/invoices", OpenInvoice));

        var result = await client.CreateInvoiceAsync(Request);

        Assert.True(result.Succeeded);
        Assert.Equal(PaymentInvoiceStatusType.Open, result.Data!.Status);
        Assert.Equal(new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero), result.Data.PeriodStartUtc);
    }

    [Fact]
    public async Task ACustomerWithNoCard_StillGetsTheInvoice_Open()
    {
        var (client, _) = Create(new StubStripeHandler()
            .Answer("/v1/invoiceitems", """{"id":"ii_1","object":"invoiceitem"}""")
            .Answer("/v1/invoices/in_1/finalize", OpenInvoice)
            .Answer("/v1/invoices/in_1/pay", """{"error":{"message":"This customer has no attached payment source or default payment method.","type":"invalid_request_error"}}""", HttpStatusCode.BadRequest)
            .Answer("/v1/invoices/in_1", OpenInvoice)
            .Answer("/v1/invoices", OpenInvoice));

        var result = await client.CreateInvoiceAsync(Request);

        Assert.True(result.Succeeded);
        Assert.Equal(PaymentInvoiceStatusType.Open, result.Data!.Status);
    }

    [Fact]
    public async Task Paying_ReportsTheRefusal_ForTheCustomerToRead()
    {
        var (client, _) = Create(new StubStripeHandler()
            .Answer("/v1/invoices/in_1/pay", """{"error":{"message":"Your card was declined.","type":"card_error","code":"card_declined"}}""", HttpStatusCode.PaymentRequired));

        var result = await client.PayInvoiceAsync("in_1");

        Assert.False(result.Succeeded);
        Assert.Contains("declined", result.Messages[0].Title);
    }

    [Fact]
    public async Task TheList_LeavesDraftsOut()
    {
        var (client, _) = Create(new StubStripeHandler()
            .Answer("/v1/invoices", $$"""{"object":"list","data":[{{OpenInvoice}},{"id":"in_2","object":"invoice","customer":"cus_1","status":"draft","currency":"brl","created":1767225600}],"has_more":true}"""));

        var result = await client.ListInvoicesAsync("cus_1");

        Assert.True(result.Succeeded);
        Assert.Single(result.Data!.Invoices);
        Assert.Equal("NUV-0001", result.Data.Invoices[0].Number);
        Assert.True(result.Data.HasMore);
    }

    [Fact]
    public async Task ThePdf_IsFetchedByTheServer_AndNamedByTheNumber()
    {
        var (client, stub) = Create(new StubStripeHandler()
            .Answer("/v1/invoices/in_1", OpenInvoice)
            .Answer("/invoice/in_1/pdf", "%PDF-1.4"));

        var result = await client.GetInvoicePdfAsync("in_1");

        Assert.True(result.Succeeded);
        Assert.Equal("NUV-0001.pdf", result.Data!.FileName);
        Assert.Equal("application/pdf", result.Data.ContentType);
        Assert.Contains(stub.Requests, r => r.Path == "/invoice/in_1/pdf");
    }

    [Fact]
    public async Task ACreditNote_RefundsToTheCard()
    {
        var (client, stub) = Create(new StubStripeHandler()
            .Answer("/v1/credit_notes", """{"id":"cn_1","object":"credit_note","invoice":"in_1","number":"NUV-0001-CN-01","currency":"brl","total":500,"status":"issued","created":1767225600,"metadata":{}}"""));

        var result = await client.CreateCreditNoteAsync(new PaymentCreditNoteRequest("in_1", 500, "refund:7", "Duplicate call"));

        Assert.True(result.Succeeded);
        Assert.Equal(500, result.Data!.AmountMinor);
        Assert.False(result.Data.IsVoid);

        var form = Uri.UnescapeDataString(stub.Requests.Single().Body!);

        Assert.Contains("refund_amount=500", form);
        Assert.Contains("invoice=in_1", form);
    }
}
