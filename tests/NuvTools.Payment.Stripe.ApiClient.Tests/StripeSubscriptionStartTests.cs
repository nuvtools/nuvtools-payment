using Microsoft.Extensions.Logging.Abstractions;
using NuvTools.Payment.DTOs.Requests;
using NuvTools.Payment.Enumerations;
using NuvTools.Payment.Stripe.ApiClient.Services;
using Stripe;

namespace NuvTools.Payment.Stripe.ApiClient.Tests;

/// <summary>A subscription started from a saved card, with no Stripe page, and what its first payment did.</summary>
public class StripeSubscriptionStartTests
{
    private const string Price = """{"object":"list","data":[{"id":"price_123","object":"price","lookup_key":"clinuv_pro_monthly"}],"has_more":false}""";

    private static StripeSubscriptionApiClient Create(StubStripeHandler stub) =>
        new(new StripeClient("sk_test_stub", httpClient: new SystemNetHttpClient(new HttpClient(stub), maxNetworkRetries: 0)),
            NullLogger<StripeSubscriptionApiClient>.Instance);

    private static string Subscription(string status, string intentStatus) =>
        """{"id":"sub_1","object":"subscription","customer":"cus_1","status":"STATUS","cancel_at_period_end":false,"metadata":{},"items":{"object":"list","data":[]},"latest_invoice":{"id":"in_1","object":"invoice","confirmation_secret":{"client_secret":"pi_1_secret_x","type":"payment_intent"},"payments":{"object":"list","data":[{"id":"inpay_1","object":"invoice_payment","payment":{"type":"payment_intent","payment_intent":{"id":"pi_1","object":"payment_intent","status":"INTENT","client_secret":"pi_1_secret_x","last_payment_error":{"message":"Your card was declined."}}}}]}}}""".Replace("STATUS", status).Replace("INTENT", intentStatus);

    private static readonly SubscriptionRequest Request = new("cus_1", "clinuv_pro_monthly", "pm_1",
        new Dictionary<string, string> { ["billing_account_id"] = "ba_1" }, "sub:ba_1:clinuv");

    [Theory]
    [InlineData("active", "succeeded", PaymentStatusType.Succeeded)]
    [InlineData("incomplete", "requires_action", PaymentStatusType.RequiresAction)]
    [InlineData("incomplete", "requires_payment_method", PaymentStatusType.Failed)]
    public async Task TheFirstPayment_SaysWhatThePageDoesNext(string status, string intentStatus, PaymentStatusType expected)
    {
        var stub = new StubStripeHandler()
            .Answer("/v1/prices", Price)
            .Answer("/v1/subscriptions", Subscription(status, intentStatus));

        var result = await Create(stub).CreateSubscriptionAsync(Request);

        Assert.True(result.Succeeded);
        Assert.Equal(expected, result.Data!.FirstPayment);
        Assert.Equal(expected == PaymentStatusType.RequiresAction ? "pi_1_secret_x" : null, result.Data.ClientSecret);

        var form = Uri.UnescapeDataString(stub.Requests.Single(r => r.Path == "/v1/subscriptions").Body!);

        Assert.Contains("payment_behavior=allow_incomplete", form);
        Assert.Contains("default_payment_method=pm_1", form);
        Assert.Contains("items[0][price]=price_123", form);
        Assert.Contains("metadata[billing_account_id]=ba_1", form);
    }

    [Fact]
    public async Task Cancelling_EndsAtThePeriodEnd_AndResumingUndoesIt()
    {
        var stub = new StubStripeHandler()
            .Answer("/v1/subscriptions/sub_1", """{"id":"sub_1","object":"subscription","customer":"cus_1","status":"active","cancel_at_period_end":true,"metadata":{},"items":{"object":"list","data":[]}}""");
        var client = Create(stub);

        var cancelled = await client.CancelSubscriptionAsync("sub_1");
        await client.ResumeSubscriptionAsync("sub_1");

        Assert.True(cancelled.Data!.CancelAtPeriodEnd);
        Assert.Contains("cancel_at_period_end=true", stub.Requests[0].Body);
        Assert.Contains("cancel_at_period_end=false", stub.Requests[1].Body);
    }
}
