namespace NuvTools.Payment.Omie.ApiClient.DTOs.Responses;

/// <summary>
/// A Service Contract of Omie as callers consume it: the number the user knows, the validity period and the
/// negotiated value, with the dates already parsed. Projected from the listing so no caller has to deal with Omie's
/// dd/MM/yyyy strings or with which key carries what.
/// </summary>
/// <param name="ContractCode">Omie's internal identifier (nCodCtr).</param>
/// <param name="ContractNumber">The contract number as the user reads it in Omie (cNumCtr).</param>
/// <param name="ClientCode">Client the contract belongs to (nCodCli).</param>
/// <param name="ValidFrom">Start of the validity period; null when Omie sent none.</param>
/// <param name="ValidTo">End of the validity period; null on contracts with no end date.</param>
/// <param name="MonthlyValue">Negotiated monthly value; null when Omie sent none.</param>
public sealed record OmieContractInfo(
    long ContractCode,
    string? ContractNumber,
    long ClientCode,
    DateOnly? ValidFrom,
    DateOnly? ValidTo,
    decimal? MonthlyValue);
