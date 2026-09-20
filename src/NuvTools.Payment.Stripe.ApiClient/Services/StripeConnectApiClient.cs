using Microsoft.Extensions.Logging;
using NuvTools.Common.ResultWrapper;
using NuvTools.Payment.Contracts;
using NuvTools.Payment.DTOs;
using Stripe;

namespace NuvTools.Payment.Stripe.ApiClient.Services;

/// <inheritdoc cref="IPayeeAccountClient" />
/// <remarks>
/// Connect Express: Stripe hosts the onboarding and owns the identity checks, which is what keeps
/// documents and bank details off the calling platform entirely.
/// </remarks>
public class StripeConnectApiClient(IStripeClient client, ILogger<StripeConnectApiClient> logger) : IPayeeAccountClient
{
    private readonly AccountService _accounts = new(client);
    private readonly AccountLinkService _accountLinks = new(client);

    public Task<IResult<string>> CreateAccountAsync(
        string countryCode, string email, IDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default) =>
        StripeCall.RunAsync(logger, nameof(CreateAccountAsync), async () =>
        {
            var account = await _accounts.CreateAsync(new AccountCreateOptions
            {
                Type = "express",
                Country = countryCode.ToUpperInvariant(),
                Email = email,
                Capabilities = new AccountCapabilitiesOptions
                {
                    Transfers = new AccountCapabilitiesTransfersOptions { Requested = true }
                },
                Metadata = metadata?.ToDictionary(e => e.Key, e => e.Value)
            }, cancellationToken: cancellationToken);

            return account.Id;
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

    /// <summary>The four fields a platform acts on, from an account object with a hundred.</summary>
    private static PayeeAccountDTO Describe(Account account) =>
        new(account.Id,
            account.ChargesEnabled,
            account.PayoutsEnabled,
            account.DetailsSubmitted,
            account.Requirements?.DisabledReason);
}
