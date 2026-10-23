using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NuvTools.Payment.DTOs.Requests;
using NuvTools.Payment.Enumerations;
using NuvTools.Payment.Stripe.ApiClient.Configuration;
using NuvTools.Payment.Stripe.ApiClient.Services;
using Stripe;

namespace NuvTools.Payment.Stripe.ApiClient.Tests;

public class StripeConnectApiClientTests
{
    private const string AccountJson = """{"id":"acct_1","object":"account"}""";

    private static (StripeConnectApiClient Client, StubStripeHandler Stub) Create(StubStripeHandler stub, Action<StripeApiClientConfig>? configure = null)
    {
        var config = new StripeApiClientConfig();
        configure?.Invoke(config);

        return (new StripeConnectApiClient(
            new StripeClient("sk_test_stub", httpClient: new SystemNetHttpClient(new HttpClient(stub), maxNetworkRetries: 0)),
            Options.Create(config),
            NullLogger<StripeConnectApiClient>.Instance), stub);
    }

    private static string Form(StubStripeHandler stub, string path) =>
        Uri.UnescapeDataString(stub.Requests.Single(r => r.Path == path).Body!).Replace('+', ' ');

    [Fact]
    public async Task WithoutADashboard_TheAccountIsCreatedByControllerProperties()
    {
        var (client, stub) = Create(new StubStripeHandler().Answer("/v1/accounts", AccountJson), c =>
        {
            c.ConnectDashboardType = "none";
            c.ConnectCapabilities.Add("card_payments");
            c.ConnectCapabilities.Add("transfers");
        });

        var result = await client.CreateAccountAsync(new PayeeAccountRequest("br", "ana@nuvtools.test"));

        Assert.True(result.Succeeded);
        Assert.Equal("acct_1", result.Data);

        var form = Form(stub, "/v1/accounts");

        Assert.Contains("controller[stripe_dashboard][type]=none", form);
        Assert.Contains("controller[fees][payer]=application", form);
        Assert.Contains("controller[losses][payments]=application", form);
        Assert.Contains("controller[requirement_collection]=stripe", form);
        Assert.Contains("country=BR", form);
        Assert.Contains("capabilities[transfers][requested]=true", form);
        Assert.DoesNotContain("type=express", form);
    }

    [Fact]
    public async Task WithoutADashboardSetting_TheAccountKeepsTheOlderType()
    {
        var (client, stub) = Create(new StubStripeHandler().Answer("/v1/accounts", AccountJson));

        await client.CreateAccountAsync("BR", "ana@nuvtools.test");

        var form = Form(stub, "/v1/accounts");

        Assert.Contains("type=express", form);
        Assert.DoesNotContain("controller[", form);
    }

    [Fact]
    public async Task APerson_IsPrefilled_AndBlankFieldsAreLeftForTheOnboarding()
    {
        var (client, stub) = Create(new StubStripeHandler().Answer("/v1/accounts", AccountJson));

        await client.CreateAccountAsync(new PayeeAccountRequest("BR", "ana@nuvtools.test")
        {
            BusinessType = PayeeBusinessType.Individual,
            Individual = new PayeeIndividual("Ana", "Souza", "ana@nuvtools.test", " ", "12345678909")
        });

        var form = Form(stub, "/v1/accounts");

        Assert.Contains("business_type=individual", form);
        Assert.Contains("individual[first_name]=Ana", form);
        Assert.Contains("individual[last_name]=Souza", form);
        Assert.Contains("individual[id_number]=12345678909", form);
        Assert.DoesNotContain("individual[phone]", form);
    }

    [Fact]
    public async Task ACompany_IsPrefilled()
    {
        var (client, stub) = Create(new StubStripeHandler().Answer("/v1/accounts", AccountJson));

        await client.CreateAccountAsync(new PayeeAccountRequest("BR", "fin@nuvtools.test")
        {
            BusinessType = PayeeBusinessType.Company,
            Company = new PayeeCompany("Nuv Tools Ltda", "38129015000127")
        });

        var form = Form(stub, "/v1/accounts");

        Assert.Contains("business_type=company", form);
        Assert.Contains("company[name]=Nuv Tools Ltda", form);
        Assert.Contains("company[tax_id]=38129015000127", form);
    }

    [Fact]
    public async Task TheOnboardingSession_IsAnAccountSessionForOnboardingOnly()
    {
        var (client, stub) = Create(new StubStripeHandler()
            .Answer("/v1/account_sessions", """{"object":"account_session","account":"acct_1","client_secret":"accs_secret_1","expires_at":1700000000}"""));

        var result = await client.CreateOnboardingSessionAsync("acct_1");

        Assert.True(result.Succeeded);
        Assert.Equal("accs_secret_1", result.Data);

        var form = Form(stub, "/v1/account_sessions");

        Assert.Contains("account=acct_1", form);
        Assert.Contains("components[account_onboarding][enabled]=true", form);
    }
}
