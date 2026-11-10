using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NuvTools.Payment.Stripe.ApiClient.Configuration;
using NuvTools.Payment.Stripe.ApiClient.Services;
using Stripe;

namespace NuvTools.Payment.Stripe.ApiClient.Tests;

public class StripePaymentMethodListTests
{
    private static (StripeCustomerApiClient Client, StubStripeHandler Stub) Create(StubStripeHandler stub) =>
        (new StripeCustomerApiClient(
            new StripeClient("sk_test_stub", httpClient: new SystemNetHttpClient(new HttpClient(stub), maxNetworkRetries: 0)),
            Options.Create(new StripeApiClientConfig()),
            NullLogger<StripeCustomerApiClient>.Instance), stub);

    [Fact]
    public async Task TheCards_ComeWithTheirExpiry_AndTheDefaultMarked()
    {
        var (client, _) = Create(new StubStripeHandler()
            .Answer("/v1/customers/cus_1/payment_methods", """{"object":"list","data":[{"id":"pm_1","object":"payment_method","type":"card","card":{"brand":"visa","last4":"4242","exp_month":12,"exp_year":2034}},{"id":"pm_2","object":"payment_method","type":"card","card":{"brand":"mastercard","last4":"4444","exp_month":1,"exp_year":2030}}],"has_more":false}""")
            .Answer("/v1/customers/cus_1", """{"id":"cus_1","object":"customer","invoice_settings":{"default_payment_method":"pm_2"}}"""));

        var result = await client.ListPaymentMethodsAsync("cus_1");

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Data!.Count);
        Assert.Equal((12, 2034, false), (result.Data[0].ExpiryMonth, result.Data[0].ExpiryYear, result.Data[0].IsDefault));
        Assert.True(result.Data[1].IsDefault);
    }

    [Fact]
    public async Task Removing_DetachesTheMethod()
    {
        var (client, stub) = Create(new StubStripeHandler()
            .Answer("/v1/payment_methods/pm_1/detach", """{"id":"pm_1","object":"payment_method"}"""));

        var result = await client.RemovePaymentMethodAsync("pm_1");

        Assert.True(result.Succeeded);
        Assert.Equal("/v1/payment_methods/pm_1/detach", stub.Requests.Single().Path);
    }
}
