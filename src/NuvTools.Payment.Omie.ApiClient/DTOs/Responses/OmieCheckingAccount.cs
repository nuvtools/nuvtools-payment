namespace NuvTools.Payment.Omie.ApiClient.DTOs.Responses;

/// <summary>A usable checking account of Omie (<c>ListarContasCorrentes</c>).</summary>
/// <param name="Code">Account identifier (<c>nCodCC</c>) — required when including a receivable.</param>
/// <param name="Description">Account description.</param>
/// <param name="AccountType">Account type (<c>tipo_conta_corrente</c>).</param>
public record OmieCheckingAccount(long Code, string Description, string? AccountType);
