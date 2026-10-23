namespace NuvTools.Payment.DTOs.Requests;

/// <summary>
/// What the application already knows about a company being onboarded as a payee, so the provider's
/// onboarding asks it to confirm it rather than type it again.
/// </summary>
/// <remarks>Every field is optional: whatever is left out, the onboarding asks for.</remarks>
/// <param name="Name">Registered name.</param>
/// <param name="TaxId">Company tax identifier — a CNPJ in Brazil — digits only.</param>
/// <param name="Phone">E.164, <c>+551130000000</c>.</param>
public record PayeeCompany(
    string? Name = null,
    string? TaxId = null,
    string? Phone = null);
