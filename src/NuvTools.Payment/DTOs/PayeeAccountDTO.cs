namespace NuvTools.Payment.DTOs;

/// <summary>
/// A payee's account at the provider, in the four answers a platform acts on.
/// </summary>
/// <remarks>
/// <b>These are the provider's answers, to be copied and not decided.</b> A platform that gates a
/// paid offer on <see cref="PayoutsEnabled"/> is reading what the provider will actually do, rather
/// than its own guess at it.
/// </remarks>
/// <param name="AccountId">The provider's identifier for the account.</param>
/// <param name="ChargesEnabled">Whether the account may be charged.</param>
/// <param name="PayoutsEnabled">
/// Whether the provider will pay the account out. Until it is true, money sent there cannot leave.
/// </param>
/// <param name="DetailsSubmitted">
/// Whether onboarding was completed, which is not the same as being enabled: the provider may still
/// be checking what was submitted.
/// </param>
/// <param name="DisabledReason">Why the provider is holding the account back, when it is.</param>
public record PayeeAccountDTO(
    string AccountId,
    bool ChargesEnabled,
    bool PayoutsEnabled,
    bool DetailsSubmitted,
    string? DisabledReason);
