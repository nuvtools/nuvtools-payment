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
/// <param name="UnitValueByServiceCode">
/// Negotiated unit price of each service in the contract, keyed by Omie's service identifier (<c>nCodServico</c>) —
/// the same key the service catalog uses, so a caller holding a service code can ask what this client pays for it.
/// Empty when the contract carries no items.
/// </param>
public sealed record OmieContractInfo(
    long ContractCode,
    string? ContractNumber,
    long ClientCode,
    DateOnly? ValidFrom,
    DateOnly? ValidTo,
    decimal? MonthlyValue,
    IReadOnlyDictionary<long, decimal> UnitValueByServiceCode)
{
    /// <summary>
    /// What this contract charges for a service, or null when the service is not in it — which is the normal case:
    /// a contract covers the services that were negotiated, and the rest stay on the standard price list.
    /// </summary>
    public decimal? UnitValueOf(long serviceCode)
        => UnitValueByServiceCode.TryGetValue(serviceCode, out var value) ? value : null;
}
