using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NuvTools.Payment.Enumerations;
using NuvTools.Payment.Stripe.ApiClient.Configuration;
using NuvTools.Payment.Stripe.ApiClient.Services;
using System.Security.Cryptography;
using System.Text;

namespace NuvTools.Payment.Stripe.ApiClient.Tests;

/// <summary>
/// The subscription events, read through the real verifier from payloads signed the way Stripe signs
/// them.
/// </summary>
public class StripeSubscriptionEventTests
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
    [InlineData("customer.subscription.created", PaymentEventType.SubscriptionChanged)]
    [InlineData("customer.subscription.updated", PaymentEventType.SubscriptionChanged)]
    [InlineData("customer.subscription.deleted", PaymentEventType.SubscriptionEnded)]
    public void ASubscriptionEvent_NamesItsSubscriptionAndCustomer(string stripeType, PaymentEventType expected)
    {
        var payload = Event(stripeType, """{"id":"sub_1","object":"subscription","customer":"cus_1","status":"active"}""");

        var result = Verifier.Verify(payload, Sign(payload));

        Assert.True(result.Succeeded);
        Assert.Equal(expected, result.Data!.Type);
        Assert.Equal("sub_1", result.Data.SubscriptionId);
        Assert.Equal("cus_1", result.Data.CustomerId);
    }

    [Theory]
    [InlineData("subscription", PaymentCheckoutType.Subscription, "sub_1")]
    [InlineData("setup", PaymentCheckoutType.Setup, null)]
    public void ACompletedCheckout_SaysWhatItWasFor(string mode, PaymentCheckoutType expected, string? subscriptionId)
    {
        var subscription = subscriptionId is null ? "null" : $"\"{subscriptionId}\"";
        var payload = Event("checkout.session.completed",
            $$"""{"id":"cs_1","object":"checkout.session","mode":"{{mode}}","customer":"cus_1","subscription":{{subscription}}}""");

        var result = Verifier.Verify(payload, Sign(payload));

        Assert.True(result.Succeeded);
        Assert.Equal(PaymentEventType.CheckoutCompleted, result.Data!.Type);
        Assert.Equal(expected, result.Data.CheckoutType);
        Assert.Equal(subscriptionId, result.Data.SubscriptionId);
    }

    [Theory]
    [InlineData("""{"id":"in_1","object":"invoice","customer":"cus_1","parent":{"type":"subscription_details","subscription_details":{"subscription":"sub_1"}}}""")]
    [InlineData("""{"id":"in_1","object":"invoice","customer":"cus_1","subscription":"sub_1"}""")]
    public void AnInvoiceEvent_NamesTheSubscriptionItBills_InEitherApiShape(string invoice)
    {
        var payload = Event("invoice.payment_failed", invoice);

        var result = Verifier.Verify(payload, Sign(payload));

        Assert.True(result.Succeeded);
        Assert.Equal(PaymentEventType.InvoicePaymentFailed, result.Data!.Type);
        Assert.Equal("sub_1", result.Data.SubscriptionId);
    }

    [Fact]
    public void AnUnsignedEvent_IsRefused()
    {
        var payload = Event("customer.subscription.updated", """{"id":"sub_1","object":"subscription"}""");

        Assert.False(Verifier.Verify(payload, "t=1,v1=00").Succeeded);
    }
}
