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
/// <para>
/// Stripe owns the identity checks. The screens that ask are Stripe's — its hosted page, or its
/// component embedded through an account session — or the application's own form, which sends the
/// details as account, person and bank-account tokens Stripe.js created in the payee's browser. Either
/// way documents, identity numbers and bank details never reach the calling platform.
/// </para>
/// <para>
/// The kind of account and the capabilities it asks for are the application's, read from
/// <see cref="StripeApiClientConfig"/>.
/// </para>
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
    private readonly AccountPersonService _persons = new(client);

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
            options.BusinessProfile = BusinessProfile(request.BusinessProfile);

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
                    AccountOnboarding = new AccountSessionComponentsAccountOnboardingOptions
                    {
                        Enabled = true,
                        // Stripe allows turning its sign-in off only where the platform keeps the
                        // account's information up to date; elsewhere it insists on it.
                        Features = PlatformCollects()
                            ? new AccountSessionComponentsAccountOnboardingFeaturesOptions { DisableStripeUserAuthentication = true }
                            : null
                    }
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

    public Task<IResult<PayeeAccountDTO>> UpdateAccountAsync(
        string accountId, PayeeAccountUpdateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return StripeCall.RunAsync(logger, nameof(UpdateAccountAsync), async () =>
        {
            // The terms acceptance is inside the account token: Stripe refuses tos_acceptance next to
            // one, and records the acceptance with the browser that created the token.
            var options = new AccountUpdateOptions
            {
                AccountToken = Blank(request.AccountToken),
                BusinessProfile = BusinessProfile(request.BusinessProfile)
            };

            // Assigned only when given: the SDK sends a null here as an empty value, which Stripe
            // reads as removing the bank account the payee is paid to.
            if (Blank(request.ExternalAccountToken) is { } bankAccount) options.ExternalAccount = bankAccount;

            var account = await _accounts.UpdateAsync(accountId, options, cancellationToken: cancellationToken);

            return Describe(account);
        });
    }

    public Task<IResult<IReadOnlyList<PayeePersonDTO>>> GetPersonsAsync(string accountId, CancellationToken cancellationToken = default) =>
        StripeCall.RunAsync<IReadOnlyList<PayeePersonDTO>>(logger, nameof(GetPersonsAsync), async () =>
        {
            var persons = new List<PayeePersonDTO>();

            await foreach (var person in _persons.ListAutoPagingAsync(accountId, new AccountPersonListOptions { Limit = 100 }, cancellationToken: cancellationToken))
                persons.Add(new PayeePersonDTO(
                    person.Id,
                    person.FirstName,
                    person.LastName,
                    person.Relationship?.Representative ?? false,
                    person.Relationship?.Owner ?? false,
                    person.Relationship?.Director ?? false,
                    person.Relationship?.Executive ?? false,
                    person.Relationship?.PercentOwnership,
                    person.Relationship?.Title));

            return persons;
        });

    public Task<IResult<string>> SavePersonAsync(
        string accountId, string personToken, string? personId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(personToken);

        return StripeCall.RunAsync(logger, nameof(SavePersonAsync), async () =>
        {
            var person = string.IsNullOrWhiteSpace(personId)
                ? await _persons.CreateAsync(accountId, new AccountPersonCreateOptions { PersonToken = personToken }, cancellationToken: cancellationToken)
                : await _persons.UpdateAsync(accountId, personId, new AccountPersonUpdateOptions { PersonToken = personToken }, cancellationToken: cancellationToken);

            return person.Id;
        });
    }

    public Task<IResult> RemovePersonAsync(string accountId, string personId, CancellationToken cancellationToken = default) =>
        StripeCall.RunAsync(logger, nameof(RemovePersonAsync), async () =>
        {
            await _persons.DeleteAsync(accountId, personId, cancellationToken: cancellationToken);
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
    /// losses. Who keeps the requirements up to date is configured separately.
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
            RequirementCollection = RequirementCollection()
        };

        if (dashboard == "none")
        {
            options.Controller.Fees = new AccountControllerFeesOptions { Payer = "application" };
            options.Controller.Losses = new AccountControllerLossesOptions { Payments = "application" };
        }
    }

    private string RequirementCollection() =>
        string.IsNullOrWhiteSpace(_config.ConnectRequirementCollection) ? "stripe" : _config.ConnectRequirementCollection.Trim().ToLowerInvariant();

    /// <summary>Whether accounts are created with the platform keeping their requirements up to date.</summary>
    private bool PlatformCollects() =>
        !string.IsNullOrWhiteSpace(_config.ConnectDashboardType) && RequirementCollection() == "application";

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

    /// <summary>The business profile, or <see langword="null"/> when nothing in it was given.</summary>
    private static AccountBusinessProfileOptions? BusinessProfile(PayeeBusinessProfile? profile)
    {
        if (profile is null) return null;

        var options = new AccountBusinessProfileOptions
        {
            Mcc = Blank(profile.Mcc),
            Url = Blank(profile.Url),
            ProductDescription = Blank(profile.ProductDescription)
        };

        if (profile.MonthlyEstimatedRevenueMinor is { } amount && Blank(profile.Currency) is { } currency)
            options.MonthlyEstimatedRevenue = new AccountBusinessProfileMonthlyEstimatedRevenueOptions
            {
                Amount = amount,
                Currency = currency.ToLowerInvariant()
            };

        return options.Mcc is null && options.Url is null && options.ProductDescription is null && options.MonthlyEstimatedRevenue is null
            ? null
            : options;
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

    /// <summary>
    /// The fields a platform acts on, from an account object with a hundred: the flags, what is still
    /// due, and the end of the bank account it pays out to. No identity data.
    /// </summary>
    private static PayeeAccountDTO Describe(Account account) =>
        new(account.Id,
            account.ChargesEnabled,
            account.PayoutsEnabled,
            account.DetailsSubmitted,
            account.Requirements?.DisabledReason)
        {
            Requirements = account.Requirements is { } requirements
                ? new PayeeRequirementsDTO(
                    [.. requirements.CurrentlyDue ?? []],
                    [.. requirements.EventuallyDue ?? []],
                    [.. requirements.PastDue ?? []],
                    [.. requirements.PendingVerification ?? []],
                    [.. (requirements.Errors ?? []).Select(e => new PayeeRequirementErrorDTO(e.Requirement, e.Code, e.Reason))],
                    requirements.CurrentDeadline is { } deadline ? new DateTimeOffset(DateTime.SpecifyKind(deadline, DateTimeKind.Utc)) : null)
                : null,
            PayoutDestination = account.ExternalAccounts?.Data?.OfType<BankAccount>()
                .OrderByDescending(e => e.DefaultForCurrency ?? false)
                .Select(e => new PayeePayoutDestinationDTO(e.BankName, e.Last4, e.Currency))
                .FirstOrDefault()
        };
}
