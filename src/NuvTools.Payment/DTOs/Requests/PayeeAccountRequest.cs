using NuvTools.Payment.Enumerations;

namespace NuvTools.Payment.DTOs.Requests;

/// <summary>
/// A payee account to create, with what the application already knows about its owner.
/// </summary>
/// <remarks>
/// The details are a prefill, not a verification: the provider's onboarding shows them for the owner
/// to confirm or correct before they accept the provider's terms. Leaving them out is always valid.
/// </remarks>
/// <param name="CountryCode">ISO 3166-1 alpha-2, the country the payee is paid in.</param>
/// <param name="Email">Where the provider writes to the payee.</param>
public record PayeeAccountRequest(string CountryCode, string Email)
{
    /// <summary>A person or a company; <see langword="null"/> lets the onboarding ask.</summary>
    public PayeeBusinessType? BusinessType { get; init; }

    /// <summary>The person's details, when <see cref="BusinessType"/> is <see cref="PayeeBusinessType.Individual"/>.</summary>
    public PayeeIndividual? Individual { get; init; }

    /// <summary>The company's details, when <see cref="BusinessType"/> is <see cref="PayeeBusinessType.Company"/>.</summary>
    public PayeeCompany? Company { get; init; }

    /// <summary>Carried on the provider's object; the caller's own identifiers belong here.</summary>
    public IDictionary<string, string>? Metadata { get; init; }
}
