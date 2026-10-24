using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NuvTools.Payment.Stripe.ApiClient.Configuration;
using NuvTools.Payment.Stripe.ApiClient.Services;
using Stripe;

namespace NuvTools.Payment.Stripe.ApiClient.Tests;

public class StripeCustomerApiClientTests
{
    private static (StripeCustomerApiClient Client, StubStripeHandler Stub) Create(StubStripeHandler stub) =>
        (new StripeCustomerApiClient(
            new StripeClient("sk_test_stub", httpClient: new SystemNetHttpClient(new HttpClient(stub), maxNetworkRetries: 0)),
            Options.Create(new StripeApiClientConfig()),
            NullLogger<StripeCustomerApiClient>.Instance), stub);

    [Fact]
    public async Task DeletingACustomer_SendsADelete_AndARefusalIsAResult()
    {
        var (client, stub) = Create(new StubStripeHandler()
            .Answer("/v1/customers/cus_1", """{"id":"cus_1","object":"customer","deleted":true}""")
            .Answer("/v1/customers/cus_2", """{"error":{"message":"No such customer: 'cus_2'","type":"invalid_request_error"}}""", System.Net.HttpStatusCode.NotFound));

        var deleted = await client.DeleteCustomerAsync("cus_1");
        var refused = await client.DeleteCustomerAsync("cus_2");

        Assert.True(deleted.Succeeded);
        Assert.Equal(HttpMethod.Delete, stub.Requests[0].Method);
        Assert.Equal("/v1/customers/cus_1", stub.Requests[0].Path);
        Assert.False(refused.Succeeded);
        Assert.Contains("No such customer", refused.Messages[0].Title);
    }
}
