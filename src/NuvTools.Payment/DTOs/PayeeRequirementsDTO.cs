namespace NuvTools.Payment.DTOs;

/// <summary>
/// What the provider still needs from a payee before it pays them, by the provider's own field names.
/// </summary>
/// <remarks>
/// <para>
/// The names are the provider's — <c>individual.address.city</c>, <c>external_account</c>,
/// <c>person_123.verification.proof_of_liveness</c> — so an application that builds its own form maps
/// them to its fields, and one it does not recognise is a reason to send the payee to the provider's
/// own onboarding rather than to guess.
/// </para>
/// <para>
/// None of these carry a value: they say which field is missing, never what it holds.
/// </para>
/// </remarks>
/// <param name="CurrentlyDue">Needed now; left unanswered past the deadline, the account is restricted.</param>
/// <param name="EventuallyDue">Needed at some point, the currently due included.</param>
/// <param name="PastDue">Already late: the account is restricted until they are answered.</param>
/// <param name="PendingVerification">Answered and being checked; nothing to do but wait.</param>
/// <param name="Errors">Answers the provider refused, and why.</param>
/// <param name="CurrentDeadline">When the currently due fields become past due, when the provider set a date.</param>
public record PayeeRequirementsDTO(
    IReadOnlyList<string> CurrentlyDue,
    IReadOnlyList<string> EventuallyDue,
    IReadOnlyList<string> PastDue,
    IReadOnlyList<string> PendingVerification,
    IReadOnlyList<PayeeRequirementErrorDTO> Errors,
    DateTimeOffset? CurrentDeadline);
