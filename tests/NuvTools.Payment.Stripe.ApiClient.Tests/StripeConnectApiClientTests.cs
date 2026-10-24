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
    public async Task WhenThePlatformCollects_TheAccountSaysSo_AndTheSessionTurnsStripesSignInOff()
    {
        var (client, stub) = Create(new StubStripeHandler()
            .Answer("/v1/accounts", AccountJson)
            .Answer("/v1/account_sessions", """{"object":"account_session","account":"acct_1","client_secret":"accs_secret_1","expires_at":1700000000}"""), c =>
        {
            c.ConnectDashboardType = "none";
            c.ConnectRequirementCollection = "application";
        });

        await client.CreateAccountAsync(new PayeeAccountRequest("BR", "ana@nuvtools.test"));
        await client.CreateOnboardingSessionAsync("acct_1");

        Assert.Contains("controller[requirement_collection]=application", Form(stub, "/v1/accounts"));
        Assert.Contains("components[account_onboarding][features][disable_stripe_user_authentication]=true", Form(stub, "/v1/account_sessions"));
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
        Assert.DoesNotContain("disable_stripe_user_authentication", form);
    }

    private const string UpdatedAccountJson = """
        {"id":"acct_1","object":"account","charges_enabled":false,"payouts_enabled":false,"details_submitted":true,
         "requirements":{"currently_due":["person_1.verification.proof_of_liveness"],"eventually_due":["person_1.verification.proof_of_liveness"],
           "past_due":[],"pending_verification":["individual.id_number"],"current_deadline":1700000000,"disabled_reason":"requirements.past_due",
           "errors":[{"requirement":"individual.dob.year","code":"invalid_dob_age_under_minimum","reason":"Too young."}]},
         "external_accounts":{"object":"list","data":[{"id":"ba_1","object":"bank_account","bank_name":"STRIPE TEST BANK","last4":"1234","currency":"brl","default_for_currency":true}]}}
        """;

    [Fact]
    public async Task AnUpdate_PassesTheTokensOn_WithoutATermsAcceptanceOfItsOwn()
    {
        var (client, stub) = Create(new StubStripeHandler().Answer("/v1/accounts/acct_1", UpdatedAccountJson));

        var result = await client.UpdateAccountAsync("acct_1", new PayeeAccountUpdateRequest
        {
            AccountToken = "ct_1",
            ExternalAccountToken = "btok_1",
            BusinessProfile = new PayeeBusinessProfile("7372", ProductDescription: "API access", MonthlyEstimatedRevenueMinor: 100000, Currency: "BRL")
        });

        Assert.True(result.Succeeded);

        var form = Form(stub, "/v1/accounts/acct_1");

        Assert.Contains("account_token=ct_1", form);
        Assert.Contains("external_account=btok_1", form);
        Assert.Contains("business_profile[mcc]=7372", form);
        Assert.Contains("business_profile[product_description]=API access", form);
        Assert.Contains("business_profile[monthly_estimated_revenue][amount]=100000", form);
        Assert.Contains("business_profile[monthly_estimated_revenue][currency]=brl", form);
        Assert.DoesNotContain("tos_acceptance", form);
        Assert.DoesNotContain("business_profile[url]", form);
    }

    [Fact]
    public async Task TheAccount_SaysWhatIsStillDue_AndOnlyTheEndOfItsBankAccount()
    {
        var (client, _) = Create(new StubStripeHandler().Answer("/v1/accounts/acct_1", UpdatedAccountJson));

        var result = await client.GetAccountAsync("acct_1");

        Assert.True(result.Succeeded);

        var requirements = result.Data!.Requirements!;

        Assert.Equal(["person_1.verification.proof_of_liveness"], requirements.CurrentlyDue);
        Assert.Equal(["individual.id_number"], requirements.PendingVerification);
        Assert.Equal("invalid_dob_age_under_minimum", Assert.Single(requirements.Errors).Code);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1700000000), requirements.CurrentDeadline);
        Assert.Equal(new DTOs.PayeePayoutDestinationDTO("STRIPE TEST BANK", "1234", "brl"), result.Data.PayoutDestination);
    }

    [Fact]
    public async Task AnUpdateWithNothingButAToken_SendsNoBusinessProfile()
    {
        var (client, stub) = Create(new StubStripeHandler().Answer("/v1/accounts/acct_1", UpdatedAccountJson));

        await client.UpdateAccountAsync("acct_1", new PayeeAccountUpdateRequest { AccountToken = "ct_1", BusinessProfile = new PayeeBusinessProfile() });

        var form = Form(stub, "/v1/accounts/acct_1");

        Assert.Contains("account_token=ct_1", form);
        Assert.DoesNotContain("business_profile", form);
        Assert.DoesNotContain("external_account", form);
    }

    [Fact]
    public async Task AnUpdateWithABankAccountAlone_LeavesTheLegalEntityAsItWas()
    {
        var (client, stub) = Create(new StubStripeHandler().Answer("/v1/accounts/acct_1", UpdatedAccountJson));

        await client.UpdateAccountAsync("acct_1", new PayeeAccountUpdateRequest { ExternalAccountToken = "btok_1" });

        Assert.Equal("external_account=btok_1", Form(stub, "/v1/accounts/acct_1"));
    }

    [Fact]
    public async Task APersonToken_RegistersANewPerson_OrChangesTheOneNamed()
    {
        var stub = new StubStripeHandler()
            .Answer("/v1/accounts/acct_1/persons/person_9", """{"id":"person_9","object":"person"}""")
            .Answer("/v1/accounts/acct_1/persons", """{"id":"person_1","object":"person"}""");
        var (client, _) = Create(stub);

        var created = await client.SavePersonAsync("acct_1", "cpt_1");
        var changed = await client.SavePersonAsync("acct_1", "cpt_2", "person_9");

        Assert.Equal("person_1", created.Data);
        Assert.Equal("person_9", changed.Data);
        Assert.Contains("person_token=cpt_1", Form(stub, "/v1/accounts/acct_1/persons"));
        Assert.Contains("person_token=cpt_2", Form(stub, "/v1/accounts/acct_1/persons/person_9"));
    }

    [Fact]
    public async Task DeletingAnAccount_SendsADelete_AndABalanceRefusalIsAResult()
    {
        var stub = new StubStripeHandler()
            .Answer("/v1/accounts/acct_1", """{"id":"acct_1","object":"account","deleted":true}""")
            .Answer("/v1/accounts/acct_2", """{"error":{"message":"This account cannot be deleted while it has a balance.","type":"invalid_request_error"}}""", System.Net.HttpStatusCode.BadRequest);
        var (client, _) = Create(stub);

        var deleted = await client.DeleteAccountAsync("acct_1");
        var refused = await client.DeleteAccountAsync("acct_2");

        Assert.True(deleted.Succeeded);
        Assert.Equal(HttpMethod.Delete, stub.Requests[0].Method);
        Assert.False(refused.Succeeded);
        Assert.Contains("balance", refused.Messages[0].Title);
    }

    [Fact]
    public async Task ThePersons_AreListedWithTheirRoles()
    {
        var (client, _) = Create(new StubStripeHandler().Answer("/v1/accounts/acct_1/persons", """
            {"object":"list","has_more":false,"data":[{"id":"person_1","object":"person","first_name":"Ana","last_name":"Souza",
              "relationship":{"representative":true,"owner":true,"director":false,"executive":true,"percent_ownership":60,"title":"CEO"}}]}
            """));

        var result = await client.GetPersonsAsync("acct_1");

        Assert.True(result.Succeeded);
        Assert.Equal(new DTOs.PayeePersonDTO("person_1", "Ana", "Souza", true, true, false, true, 60m, "CEO"), Assert.Single(result.Data!));
    }

    [Fact]
    public async Task RemovingAPerson_DeletesIt_AndARefusalIsAResult()
    {
        var stub = new StubStripeHandler()
            .Answer("/v1/accounts/acct_1/persons/person_1", """{"id":"person_1","object":"person","deleted":true}""")
            .Answer("/v1/accounts/acct_1/persons/person_2", """{"error":{"message":"The representative cannot be removed.","type":"invalid_request_error"}}""", System.Net.HttpStatusCode.BadRequest);
        var (client, _) = Create(stub);

        var removed = await client.RemovePersonAsync("acct_1", "person_1");
        var refused = await client.RemovePersonAsync("acct_1", "person_2");

        Assert.True(removed.Succeeded);
        Assert.Equal(HttpMethod.Delete, stub.Requests[0].Method);
        Assert.False(refused.Succeeded);
        Assert.Contains("representative", refused.Messages[0].Title);
    }
}
