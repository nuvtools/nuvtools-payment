using Microsoft.Extensions.Logging;
using NuvTools.Common.ResultWrapper;
using NuvTools.Payment.Contracts;
using Microsoft.Extensions.Options;
using NuvTools.Payment.DTOs;
using NuvTools.Payment.DTOs.Requests;
using NuvTools.Payment.Enumerations;
using NuvTools.Payment.Stripe.ApiClient.Configuration;
using Stripe;

namespace NuvTools.Payment.Stripe.ApiClient.Services;

/// <inheritdoc cref="IPayeeAccountClient" />
/// <remarks>
/// Stripe owns the onboarding and the identity checks — on its hosted page, or embedded in the
/// application through an account session — which is what keeps documents and bank details off the
/// calling platform entirely. The kind of account and the capabilities it asks for
/// are the application's, read from <see cref="StripeApiClientConfig"/>.
/// </remarks>
public class StripeConnectApiClient(
    IStripeClient client,
    IOptions<StripeApiClientConfig> config,
    ILogger<StripeConnectApiClient> logger) : IPayeeAccountClient
{
    private readonly StripeApiClientConfig _config = config.Value;
    private readonly AccountService _accounts = new(client);
    private readonly AccountLinkService _accountLinks = new(client);
    private readonly AccountSessionService _accountSessions = new(client);

    public Task<IResult<string>> CreateAccountAsync(
        string countryCode, string email, IDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default) =>
        CreateAccountAsync(new PayeeAccountRequest(countryCode, email) { Metadata = metadata }, cancellationToken);

    public Task<IResult<string>> CreateAccountAsync(PayeeAccountRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return StripeCall.RunAsync(logger, nameof(CreateAccountAsync), async () =>
        {
            var options = new AccountCreateOptions
            {
                Country = request.CountryCode.ToUpperInvariant(),
                Email = request.Email,
                Metadata = request.Metadata?.ToDictionary(e => e.Key, e => e.Value)
            };

            ApplyKind(options);
            ApplyPrefill(options, request);

            // By name rather than through the SDK's typed properties, so a capability Stripe adds
            // is one the application can ask for without a new release of this package.
            foreach (var capability in Capabilities())
                options.AddExtraParam($"capabilities[{capability}][requested]", "true");

            var account = await _accounts.CreateAsync(options, cancellationToken: cancellationToken);

            return account.Id;
        });
    }

    public Task<IResult<string>> CreateOnboardingSessionAsync(string accountId, CancellationToken cancellationToken = default) =>
        StripeCall.RunAsync(logger, nameof(CreateOnboardingSessionAsync), async () =>
        {
            var session = await _accountSessions.CreateAsync(new AccountSessionCreateOptions
            {
                Account = accountId,
                Components = new AccountSessionComponentsOptions
                {
                    AccountOnboarding = new AccountSessionComponentsAccountOnboardingOptions { Enabled = true }
                }
            }, cancellationToken: cancellationToken);

            return session.ClientSecret;
        });

    public Task<IResult<string>> CreateOnboardingLinkAsync(
        string accountId, string refreshUrl, string returnUrl, CancellationToken cancellationToken = default) =>
        StripeCall.RunAsync(logger, nameof(CreateOnboardingLinkAsync), async () =>
        {
            var link = await _accountLinks.CreateAsync(new AccountLinkCreateOptions
            {
                Account = accountId,
                RefreshUrl = refreshUrl,
                ReturnUrl = returnUrl,
                Type = "account_onboarding"
            }, cancellationToken: cancellationToken);

            return link.Url;
        });

    public Task<IResult<PayeeAccountDTO>> GetAccountAsync(string accountId, CancellationToken cancellationToken = default) =>
        StripeCall.RunAsync(logger, nameof(GetAccountAsync), async () =>
        {
            var account = await _accounts.GetAsync(accountId, cancellationToken: cancellationToken);

            return Describe(account);
        });

    /// <summary>
    /// The kind of account: by controller properties when a dashboard is configured, by the older
    /// account type otherwise.
    /// </summary>
    /// <remarks>
    /// Without a Stripe Dashboard, Stripe requires the platform to pay the fees and answer for the
    /// losses; it still collects the requirements itself, so its onboarding stays the one that asks.
    /// </remarks>
    private void ApplyKind(AccountCreateOptions options)
    {
        if (string.IsNullOrWhiteSpace(_config.ConnectDashboardType))
        {
            options.Type = _config.ConnectAccountType;
            return;
        }

        var dashboard = _config.ConnectDashboardType.Trim().ToLowerInvariant();

        options.Controller = new AccountControllerOptions
        {
            StripeDashboard = new AccountControllerStripeDashboardOptions { Type = dashboard },
            RequirementCollection = "stripe"
        };

        if (dashboard == "none")
        {
            options.Controller.Fees = new AccountControllerFeesOptions { Payer = "application" };
            options.Controller.Losses = new AccountControllerLossesOptions { Payments = "application" };
        }
    }

    /// <summary>What the application already knows, for the onboarding to show rather than ask.</summary>
    private static void ApplyPrefill(AccountCreateOptions options, PayeeAccountRequest request)
    {
        switch (request.BusinessType)
        {
            case PayeeBusinessType.Individual:
                options.BusinessType = "individual";

                if (request.Individual is { } person)
                    options.Individual = new AccountIndividualOptions
                    {
                        FirstName = Blank(person.FirstName),
                        LastName = Blank(person.LastName),
                        Email = Blank(person.Email),
                        Phone = Blank(person.Phone),
                        IdNumber = Blank(person.TaxId)
                    };
                break;

            case PayeeBusinessType.Company:
                options.BusinessType = "company";

                if (request.Company is { } company)
                    options.Company = new AccountCompanyOptions
                    {
                        Name = Blank(company.Name),
                        TaxId = Blank(company.TaxId),
                        Phone = Blank(company.Phone)
                    };
                break;
        }
    }

    /// <summary>Stripe refuses an empty string where it accepts an absent field.</summary>
    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>
    /// What the application configured, or transfers alone: the least an account that is paid by
    /// transfer needs.
    /// </summary>
    private IEnumerable<string> Capabilities() =>
        _config.ConnectCapabilities.Count > 0
            ? _config.ConnectCapabilities.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim()).Distinct()
            : ["transfers"];

    /// <summary>The four fields a platform acts on, from an account object with a hundred.</summary>
    private static PayeeAccountDTO Describe(Account account) =>
        new(account.Id,
            account.ChargesEnabled,
            account.PayoutsEnabled,
            account.DetailsSubmitted,
            account.Requirements?.DisabledReason);
}
