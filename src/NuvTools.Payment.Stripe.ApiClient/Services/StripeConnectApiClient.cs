using Microsoft.Extensions.Logging;
using NuvTools.Common.ResultWrapper;
using NuvTools.Payment.Contracts;
using Microsoft.Extensions.Options;
using NuvTools.Payment.DTOs;
using NuvTools.Payment.Stripe.ApiClient.Configuration;
using Stripe;

namespace NuvTools.Payment.Stripe.ApiClient.Services;

/// <inheritdoc cref="IPayeeAccountClient" />
/// <remarks>
/// Stripe hosts the onboarding and owns the identity checks, which is what keeps documents and bank
/// details off the calling platform entirely. The kind of account and the capabilities it asks for
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

    public Task<IResult<string>> CreateAccountAsync(
        string countryCode, string email, IDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default) =>
        StripeCall.RunAsync(logger, nameof(CreateAccountAsync), async () =>
        {
            var options = new AccountCreateOptions
            {
                Type = _config.ConnectAccountType,
                Country = countryCode.ToUpperInvariant(),
                Email = email,
                Metadata = metadata?.ToDictionary(e => e.Key, e => e.Value)
            };

            // By name rather than through the SDK's typed properties, so a capability Stripe adds
            // is one the application can ask for without a new release of this package.
            foreach (var capability in Capabilities())
                options.AddExtraParam($"capabilities[{capability}][requested]", "true");

            var account = await _accounts.CreateAsync(options, cancellationToken: cancellationToken);

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
