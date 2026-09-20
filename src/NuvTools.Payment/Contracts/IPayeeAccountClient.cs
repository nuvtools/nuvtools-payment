using NuvTools.Common.ResultWrapper;
using NuvTools.Payment.DTOs;

namespace NuvTools.Payment.Contracts;

/// <summary>
/// The receiving side: an account the platform can pay, and the onboarding that makes it payable.
/// </summary>
/// <remarks>
/// A marketplace pays people it has not vetted itself. The provider collects the identity and bank
/// details, decides whether the account may be paid out, and hosts the screens that ask — so none of
/// that reaches the platform, which is the point of using one.
/// </remarks>
public interface IPayeeAccountClient
{
    /// <summary>Creates an account able to receive transfers, and returns the provider's identifier for it.</summary>
    /// <param name="countryCode">ISO 3166-1 alpha-2, the country the payee is paid in.</param>
    /// <param name="email">Where the provider writes to the payee.</param>
    /// <param name="metadata">Carried on the provider's object; the caller's own identifiers belong here.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    Task<IResult<string>> CreateAccountAsync(
        string countryCode,
        string email,
        IDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// A link to the provider's hosted onboarding. Single-use and short-lived by design, so it is
    /// created when the payee asks rather than stored.
    /// </summary>
    /// <param name="accountId">The account being onboarded.</param>
    /// <param name="refreshUrl">Where the provider sends the payee when the link has expired.</param>
    /// <param name="returnUrl">Where it returns them when they are done.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    Task<IResult<string>> CreateOnboardingLinkAsync(
        string accountId,
        string refreshUrl,
        string returnUrl,
        CancellationToken cancellationToken = default);

    /// <summary>What the provider currently says about the account.</summary>
    /// <param name="accountId">The account to read.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    Task<IResult<PayeeAccountDTO>> GetAccountAsync(
        string accountId,
        CancellationToken cancellationToken = default);
}
