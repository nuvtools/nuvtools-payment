namespace NuvTools.Payment.DTOs.Requests;

/// <summary>
/// What the application already knows about a person being onboarded as a payee, so the provider's
/// onboarding asks them to confirm it rather than type it again.
/// </summary>
/// <remarks>Every field is optional: whatever is left out, the onboarding asks for.</remarks>
/// <param name="FirstName">Given name, as on their identity document.</param>
/// <param name="LastName">Family name, as on their identity document.</param>
/// <param name="Email">Where the provider writes to them.</param>
/// <param name="Phone">E.164, <c>+5511999990000</c>.</param>
/// <param name="TaxId">Their personal tax identifier — a CPF in Brazil — digits only.</param>
public record PayeeIndividual(
    string? FirstName = null,
    string? LastName = null,
    string? Email = null,
    string? Phone = null,
    string? TaxId = null);
