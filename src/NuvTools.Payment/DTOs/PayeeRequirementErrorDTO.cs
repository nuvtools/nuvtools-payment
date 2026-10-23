namespace NuvTools.Payment.DTOs;

/// <summary>An answer the provider refused.</summary>
/// <param name="Requirement">The field, by the provider's name.</param>
/// <param name="Code">The provider's code for the refusal, stable enough to map to a message of one's own.</param>
/// <param name="Reason">The provider's explanation, in its words.</param>
public record PayeeRequirementErrorDTO(string Requirement, string Code, string Reason);
