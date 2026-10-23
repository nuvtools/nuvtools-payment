using NuvTools.Common.ResultWrapper;
using NuvTools.Payment.DTOs;
using NuvTools.Payment.DTOs.Requests;

namespace NuvTools.Payment.Contracts;

/// <summary>
/// The receiving side: an account the platform can pay, and the onboarding that makes it payable.
/// </summary>
/// <remarks>
/// A marketplace pays people it has not vetted itself. The provider verifies the identity and bank
/// details and decides whether the account may be paid out. The screens that ask are the provider's
/// own — hosted, or embedded in the application — or the application's, which then sends the
/// details as tokens the payee's browser obtained, so they never reach the platform either way.
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
    /// Creates an account able to receive transfers, prefilled with what the application knows about
    /// its owner, and returns the provider's identifier for it.
    /// </summary>
    /// <param name="request">The country, the address, and the optional prefill.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    Task<IResult<string>> CreateAccountAsync(
        PayeeAccountRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// A short-lived secret that lets the provider's embeddable onboarding run inside the
    /// application's own page, for this account only.
    /// </summary>
    /// <remarks>
    /// The alternative to <see cref="CreateOnboardingLinkAsync"/>: the payee never leaves the
    /// application. The secret is handed to the browser and used at once, never stored.
    /// </remarks>
    /// <param name="accountId">The account being onboarded.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    Task<IResult<string>> CreateOnboardingSessionAsync(
        string accountId,
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

    /// <summary>
    /// Changes the account: its legal entity, terms acceptance and bank account as tokens the payee's
    /// browser obtained from the provider, and what it does. Answers the account as it then stands.
    /// </summary>
    /// <param name="accountId">The account to change.</param>
    /// <param name="request">The tokens and the business profile; anything left out stays as it was.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    Task<IResult<PayeeAccountDTO>> UpdateAccountAsync(
        string accountId,
        PayeeAccountUpdateRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>The people registered on a company account.</summary>
    /// <param name="accountId">The company's account.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    Task<IResult<IReadOnlyList<PayeePersonDTO>>> GetPersonsAsync(
        string accountId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a person on a company account, or changes one already there, from a provider person
    /// token the payee's browser obtained. Answers the person's identifier.
    /// </summary>
    /// <param name="accountId">The company's account.</param>
    /// <param name="personToken">The person's details, as a provider person token.</param>
    /// <param name="personId">The person to change; <see langword="null"/> registers a new one.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    Task<IResult<string>> SavePersonAsync(
        string accountId,
        string personToken,
        string? personId = null,
        CancellationToken cancellationToken = default);

    /// <summary>Removes a person registered on a company account by mistake.</summary>
    /// <param name="accountId">The company's account.</param>
    /// <param name="personId">The person to remove.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    Task<IResult> RemovePersonAsync(
        string accountId,
        string personId,
        CancellationToken cancellationToken = default);

    /// <summary>What the provider currently says about the account.</summary>
    /// <param name="accountId">The account to read.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    Task<IResult<PayeeAccountDTO>> GetAccountAsync(
        string accountId,
        CancellationToken cancellationToken = default);
}
