using Microsoft.Extensions.Logging.Abstractions;
using NuvTools.Payment.DTOs.Requests;
using NuvTools.Payment.Enumerations;
using NuvTools.Payment.Stripe.ApiClient.Services;
using Stripe;
using System.Net;

namespace NuvTools.Payment.Stripe.ApiClient.Tests;

public class StripeSubscriptionApiClientTests
{
    private static (StripeSubscriptionApiClient Client, StubStripeHandler Stub) Create(StubStripeHandler stub) =>
        (new StripeSubscriptionApiClient(
            new StripeClient("sk_test_stub", httpClient: new SystemNetHttpClient(new HttpClient(stub), maxNetworkRetries: 0)),
            NullLogger<StripeSubscriptionApiClient>.Instance), stub);

    private static readonly SubscriptionCheckoutRequest Request = new(
        "cus_1", "clinuv_pro_monthly", "https://account.test/ok", "https://account.test/cancel",
        "ba_1", new Dictionary<string, string> { ["billing_account_id"] = "ba_1" }, "idem-1");

    [Fact]
    public async Task Checkout_FindsThePriceByItsLookupKey_AndOpensASubscriptionSession()
    {
        var (client, stub) = Create(new StubStripeHandler()
            .Answer("/v1/prices", """{"object":"list","data":[{"id":"price_123","object":"price","lookup_key":"clinuv_pro_monthly"}],"has_more":false}""")
            .Answer("/v1/checkout/sessions", """{"id":"cs_1","object":"checkout.session","url":"https://checkout.stripe.test/cs_1"}"""));

        var result = await client.CreateCheckoutPageAsync(Request);

        Assert.True(result.Succeeded);
        Assert.Equal("https://checkout.stripe.test/cs_1", result.Data);

        var session = stub.Requests.Single(r => r.Path == "/v1/checkout/sessions").Body!;
        var form = Uri.UnescapeDataString(session);

        Assert.Contains("mode=subscription", form);
        Assert.Contains("line_items[0][price]=price_123", form);
        Assert.Contains("customer=cus_1", form);
        Assert.Contains("client_reference_id=ba_1", form);
        Assert.Contains("subscription_data[metadata][billing_account_id]=ba_1", form);
    }

    [Fact]
    public async Task Checkout_ForALookupKeyWithNoActivePrice_IsNotFound_AndOpensNothing()
    {
        var (client, stub) = Create(new StubStripeHandler()
            .Answer("/v1/prices", """{"object":"list","data":[],"has_more":false}"""));

        var result = await client.CreateCheckoutPageAsync(Request);

        Assert.False(result.Succeeded);
        Assert.True(result.ContainsNotFound);
        Assert.DoesNotContain(stub.Requests, r => r.Path == "/v1/checkout/sessions");
    }

    [Fact]
    public async Task Checkout_ReportsStripesRefusal()
    {
        var (client, _) = Create(new StubStripeHandler()
            .Answer("/v1/prices", """{"error":{"message":"Invalid API Key provided.","type":"invalid_request_error"}}""", HttpStatusCode.Unauthorized));

        var result = await client.CreateCheckoutPageAsync(Request);

        Assert.False(result.Succeeded);
        Assert.False(result.ContainsNotFound);
    }

    [Fact]
    public async Task ManagementPage_IsAPortalSessionForTheCustomer()
    {
        var (client, stub) = Create(new StubStripeHandler()
            .Answer("/v1/billing_portal/sessions", """{"id":"bps_1","object":"billing_portal.session","url":"https://billing.stripe.test/p/1"}"""));

        var result = await client.CreateManagementPageAsync("cus_1", "https://account.test/billing");

        Assert.True(result.Succeeded);
        Assert.Equal("https://billing.stripe.test/p/1", result.Data);
        Assert.Contains("customer=cus_1", Uri.UnescapeDataString(stub.Requests.Single().Body!));
    }

    [Fact]
    public async Task Subscription_IsReadWithItsStatusPriceKeyPeriodAndMetadata()
    {
        var (client, _) = Create(new StubStripeHandler().Answer("/v1/subscriptions/sub_1", """
            {
              "id": "sub_1", "object": "subscription", "customer": "cus_1", "status": "past_due",
              "cancel_at_period_end": true, "metadata": { "billing_account_id": "ba_1" },
              "items": { "object": "list", "data": [ {
                "id": "si_1", "object": "subscription_item", "current_period_end": 1767225600,
                "price": { "id": "price_123", "object": "price", "lookup_key": "clinuv_pro_monthly" }
              } ] }
            }
            """));

        var result = await client.GetSubscriptionAsync("sub_1");

        Assert.True(result.Succeeded);

        var subscription = result.Data!;

        Assert.Equal("cus_1", subscription.CustomerId);
        Assert.Equal(PaymentSubscriptionStatusType.PastDue, subscription.Status);
        Assert.Equal("clinuv_pro_monthly", subscription.PriceLookupKey);
        Assert.Equal(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), subscription.CurrentPeriodEndUtc);
        Assert.True(subscription.CancelAtPeriodEnd);
        Assert.Equal("ba_1", subscription.Metadata["billing_account_id"]);
    }

    private const string TwoItems = """
        {
          "id": "sub_1", "object": "subscription", "customer": "cus_1", "status": "active",
          "items": { "object": "list", "data": [
            { "id": "si_base", "object": "subscription_item", "quantity": 1, "current_period_end": 1767225600,
              "price": { "id": "price_base", "object": "price", "lookup_key": "clinuv_pro_monthly" } },
            { "id": "si_seat", "object": "subscription_item", "quantity": 3, "current_period_end": 1767225600,
              "price": { "id": "price_seat", "object": "price", "lookup_key": "clinuv_professional_seat" } }
          ] }
        }
        """;

    private const string BaseOnly = """
        {
          "id": "sub_1", "object": "subscription", "customer": "cus_1", "status": "active",
          "items": { "object": "list", "data": [
            { "id": "si_base", "object": "subscription_item", "quantity": 1, "current_period_end": 1767225600,
              "price": { "id": "price_base", "object": "price", "lookup_key": "clinuv_pro_monthly" } }
          ] }
        }
        """;

    [Fact]
    public async Task Subscription_ListsEveryItemWithItsQuantity()
    {
        var (client, _) = Create(new StubStripeHandler().Answer("/v1/subscriptions/sub_1", TwoItems));

        var result = await client.GetSubscriptionAsync("sub_1");

        Assert.Equal("clinuv_pro_monthly", result.Data!.PriceLookupKey);
        Assert.Equal(
            [new("clinuv_pro_monthly", 1), new("clinuv_professional_seat", 3)],
            result.Data.Items!);
    }

    [Fact]
    public async Task ItemQuantity_OfAPriceAlreadyCarried_UpdatesThatItem_WithProration()
    {
        var (client, stub) = Create(new StubStripeHandler()
            .Answer("/v1/subscriptions/sub_1", TwoItems)
            .Answer("/v1/subscription_items/si_seat", """{"id":"si_seat","object":"subscription_item","quantity":4}"""));

        var result = await client.SetItemQuantityAsync("sub_1", "clinuv_professional_seat", 4, "idem-seat");

        Assert.True(result.Succeeded);

        var update = stub.Requests.Single(r => r.Method == HttpMethod.Post && r.Path == "/v1/subscription_items/si_seat");
        var form = Uri.UnescapeDataString(update.Body!);

        Assert.Contains("quantity=4", form);
        Assert.Contains("proration_behavior=create_prorations", form);
    }

    [Fact]
    public async Task ItemQuantity_OfANewPrice_AddsTheItem_FoundByLookupKey()
    {
        var (client, stub) = Create(new StubStripeHandler()
            .Answer("/v1/subscriptions/sub_1", BaseOnly)
            .Answer("/v1/prices", """{"object":"list","data":[{"id":"price_seat","object":"price","lookup_key":"clinuv_professional_seat"}],"has_more":false}""")
            .Answer("/v1/subscription_items", """{"id":"si_seat","object":"subscription_item","quantity":1}"""));

        var result = await client.SetItemQuantityAsync("sub_1", "clinuv_professional_seat", 1);

        Assert.True(result.Succeeded);

        var create = stub.Requests.Single(r => r.Method == HttpMethod.Post && r.Path == "/v1/subscription_items");
        var form = Uri.UnescapeDataString(create.Body!);

        Assert.Contains("subscription=sub_1", form);
        Assert.Contains("price=price_seat", form);
        Assert.Contains("quantity=1", form);
    }

    [Fact]
    public async Task ItemQuantity_OfZero_TakesTheItemOff()
    {
        var (client, stub) = Create(new StubStripeHandler()
            .Answer("/v1/subscriptions/sub_1", TwoItems)
            .Answer("/v1/subscription_items/si_seat", """{"id":"si_seat","object":"subscription_item","deleted":true}"""));

        var result = await client.SetItemQuantityAsync("sub_1", "clinuv_professional_seat", 0);

        Assert.True(result.Succeeded);
        Assert.Contains(stub.Requests, r => r.Method == HttpMethod.Delete && r.Path == "/v1/subscription_items/si_seat");
    }

    [Fact]
    public async Task ItemQuantity_AlreadyRight_ChangesNothing()
    {
        var (client, stub) = Create(new StubStripeHandler().Answer("/v1/subscriptions/sub_1", TwoItems));

        var result = await client.SetItemQuantityAsync("sub_1", "clinuv_professional_seat", 3);

        Assert.True(result.Succeeded);
        Assert.DoesNotContain(stub.Requests, r => r.Path.StartsWith("/v1/subscription_items", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ItemQuantity_ForALookupKeyWithNoActivePrice_IsNotFound()
    {
        var (client, stub) = Create(new StubStripeHandler()
            .Answer("/v1/subscriptions/sub_1", BaseOnly)
            .Answer("/v1/prices", """{"object":"list","data":[],"has_more":false}"""));

        var result = await client.SetItemQuantityAsync("sub_1", "clinuv_professional_seat", 2);

        Assert.True(result.ContainsNotFound);
        Assert.DoesNotContain(stub.Requests, r => r.Path.StartsWith("/v1/subscription_items", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ItemQuantity_ReportsStripesRefusal()
    {
        var (client, _) = Create(new StubStripeHandler()
            .Answer("/v1/subscriptions/sub_1", TwoItems)
            .Answer("/v1/subscription_items/si_seat", """{"error":{"message":"This subscription is canceled.","type":"invalid_request_error"}}""", HttpStatusCode.BadRequest));

        var result = await client.SetItemQuantityAsync("sub_1", "clinuv_professional_seat", 5);

        Assert.False(result.Succeeded);
        Assert.Contains("This subscription is canceled.", result.Messages.Select(m => m.Title));
    }
}
