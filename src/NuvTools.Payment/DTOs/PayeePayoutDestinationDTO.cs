namespace NuvTools.Payment.DTOs;

/// <summary>
/// Where the provider pays a payee out, in the little a platform may show of it.
/// </summary>
/// <remarks>Never the full account number: the provider keeps that, and returns only its end.</remarks>
/// <param name="BankName">The bank, as the provider names it.</param>
/// <param name="Last4">The last four digits of the account number.</param>
/// <param name="Currency">ISO 4217, lower case as the provider writes it.</param>
public record PayeePayoutDestinationDTO(string? BankName, string? Last4, string? Currency);
