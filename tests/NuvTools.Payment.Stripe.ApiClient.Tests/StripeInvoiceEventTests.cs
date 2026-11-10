using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NuvTools.Payment.Enumerations;
using NuvTools.Payment.Stripe.ApiClient.Configuration;
using NuvTools.Payment.Stripe.ApiClient.Services;
using System.Security.Cryptography;
using System.Text;

namespace NuvTools.Payment.Stripe.ApiClient.Tests;

/// <summary>The invoice and credit note events name the invoice they are about.</summary>
public class StripeInvoiceEventTests
{
    private const string Secret = "whsec_test_secret";

    private static readonly StripeWebhookVerifier Verifier = new(
        Options.Create(new StripeApiClientConfig { WebhookSecret = Secret }),
        NullLogger<StripeWebhookVerifier>.Instance);

    private static string Event(string type, string dataObject) =>
        $$$"""{"id":"evt_1","object":"event","api_version":"2025-09-30.clover","created":1767225600,"livemode":false,"pending_webhooks":1,"type":"{{{type}}}","data":{"object":{{{dataObject}}}}}""";

    private static string Sign(string payload)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signature = Convert.ToHexStringLower(
            HMACSHA256.HashData(Encoding.UTF8.GetBytes(Secret), Encoding.UTF8.GetBytes($"{timestamp}.{payload}")));

        return $"t={timestamp},v1={signature}";
    }

    [Theory]
    [InlineData("invoice.finalized", PaymentEventType.InvoiceFinalized)]
    [InlineData("invoice.paid", PaymentEventType.InvoicePaid)]
    [InlineData("invoice.payment_action_required", PaymentEventType.InvoicePaymentActionRequired)]
    [InlineData("invoice.voided", PaymentEventType.InvoiceVoided)]
    [InlineData("invoice.marked_uncollectible", PaymentEventType.InvoiceUncollectible)]
    public void AnInvoiceEvent_NamesItsInvoice(string stripeType, PaymentEventType expected)
    {
        var payload = Event(stripeType, """{"id":"in_1","object":"invoice","customer":"cus_1","status":"open"}""");

        var result = Verifier.Verify(payload, Sign(payload));

        Assert.True(result.Succeeded);
        Assert.Equal(expected, result.Data!.Type);
        Assert.Equal("in_1", result.Data.InvoiceId);
        Assert.Equal("cus_1", result.Data.CustomerId);
    }

    [Fact]
    public void ACreditNote_NamesTheInvoiceItCredits()
    {
        var payload = Event("credit_note.created", """{"id":"cn_1","object":"credit_note","invoice":"in_1","customer":"cus_1"}""");

        var result = Verifier.Verify(payload, Sign(payload));

        Assert.Equal(PaymentEventType.CreditNoteIssued, result.Data!.Type);
        Assert.Equal("cn_1", result.Data.ObjectId);
        Assert.Equal("in_1", result.Data.InvoiceId);
    }

    [Fact]
    public void ARemovedCard_IsReported()
    {
        var payload = Event("payment_method.detached", """{"id":"pm_1","object":"payment_method","customer":null}""");

        Assert.Equal(PaymentEventType.PaymentMethodRemoved, Verifier.Verify(payload, Sign(payload)).Data!.Type);
    }
}
