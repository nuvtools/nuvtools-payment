using System.Globalization;
using Microsoft.Extensions.Caching.Memory;
using NuvTools.Common.ResultWrapper;
using NuvTools.Payment.Omie.ApiClient.Contracts;
using NuvTools.Payment.Omie.ApiClient.DTOs.Responses;
using NuvTools.Payment.Omie.ApiClient.Resources;

namespace NuvTools.Payment.Omie.ApiClient.Services;

/// <summary>
/// Omie Service Contracts, read whole (<c>ListarContratos</c>, paginated) and indexed by the contract number
/// (<c>cNumCtr</c>) — the number a caller's own configuration stores, because it is what the user reads in Omie.
/// A contract is a price registry: it carries the negotiated value and the validity period of one client, and is
/// never billed through.
/// <para>
/// Listed whole and cached for <see cref="CacheDuration"/>, never consulted one by one. A screen that shows contracts
/// in a grid would otherwise ask Omie once per row, and Omie answers a burst of repeated calls with
/// "Consumo redundante detectado" — which blocks the app_key for every other integration, not just the grid.
/// </para>
/// </summary>
public class OmieContractProvider(IOmieApiClient omie, IMemoryCache cache)
{
    private const int ContractsPerPage = 100;

    private const string CacheKey = "NuvTools:Omie:Contracts";

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Contracts indexed by <c>cNumCtr</c>. With <paramref name="forceRefresh"/> the cache is ignored and rewritten
    /// with whatever Omie returns — the explicit "reload from ERP" of a screen, not the screen's own load.
    /// </summary>
    public async Task<IResult<IReadOnlyDictionary<string, OmieContractInfo>>> GetByNumberAsync(
        bool forceRefresh, CancellationToken cancellationToken = default)
    {
        if (!forceRefresh
            && cache.TryGetValue<IReadOnlyDictionary<string, OmieContractInfo>>(CacheKey, out var cached)
            && cached is not null)
            return Result<IReadOnlyDictionary<string, OmieContractInfo>>.Success(cached);

        // Indexed by the normalized number (see NormalizeNumber): the number is typed by hand into the caller's
        // configuration, and punctuation, spacing and casing are exactly where hand-typing diverges from what Omie
        // stores. The original number is kept in the projected record, for showing back to the user.
        var contracts = new Dictionary<string, OmieContractInfo>(StringComparer.Ordinal);
        var page = 1;

        while (true)
        {
            var listResult = await omie.ListContractsAsync(page, ContractsPerPage, cancellationToken);
            if (!listResult.Succeeded || listResult.Data is null)
                return Result<IReadOnlyDictionary<string, OmieContractInfo>>.Fail(
                    string.Format(CultureInfo.CurrentCulture, Messages.OmieContractListFailedX, listResult.Message));

            foreach (var contract in listResult.Data.Contracts ?? [])
            {
                var header = contract.Header;
                if (header is null) continue;

                var info = new OmieContractInfo(
                    header.ContractCode,
                    header.ContractNumber?.Trim(),
                    header.ClientCode,
                    ParseDate(header.ValidFrom),
                    ParseDate(header.ValidTo),
                    header.MonthlyValue);

                // Indexed by number and also by the internal code as text: a caller that stored nCodCtr instead of
                // cNumCtr still finds its contract, and no caller needs to know which of the two it kept.
                var number = NormalizeNumber(info.ContractNumber);
                if (number.Length > 0)
                    contracts[number] = info;

                var code = NormalizeNumber(header.ContractCode.ToString(CultureInfo.InvariantCulture));
                if (!contracts.ContainsKey(code))
                    contracts[code] = info;
            }

            if (listResult.Data.TotalPages <= page) break;
            page++;
        }

        var indexed = (IReadOnlyDictionary<string, OmieContractInfo>)contracts;
        cache.Set(CacheKey, indexed, CacheDuration);
        return Result<IReadOnlyDictionary<string, OmieContractInfo>>.Success(indexed);
    }

    /// <summary>
    /// One contract by the number (or internal code) the caller stored. Reads the same cached listing — asking for
    /// several contracts in a row costs Omie a single call, which is the whole point of listing instead of consulting.
    /// </summary>
    public async Task<IResult<OmieContractInfo?>> GetAsync(string contractNumber, bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        var contracts = await GetByNumberAsync(forceRefresh, cancellationToken);
        if (!contracts.Succeeded || contracts.Data is null)
            return Result<OmieContractInfo?>.Fail(contracts.Messages);

        return Result<OmieContractInfo?>.Success(
            data: contracts.Data.TryGetValue(NormalizeNumber(contractNumber), out var contract) ? contract : null);
    }

    /// <summary>
    /// The key both sides of the match are reduced to: letters and digits only, upper-cased. Everything else —
    /// spaces, dots, slashes, hyphens — is dropped.
    /// <para>
    /// The contract number is read off a screen and typed into the caller's configuration by hand, so "CTR 001/2026",
    /// "ctr-001-2026" and "CTR0012026" are the same contract for the person typing it, and matching them literally
    /// would report a contract that plainly exists in Omie as missing. Callers must run their stored number through
    /// this same function — <see cref="GetAsync"/> does it for them.
    /// </para>
    /// </summary>
    public static string NormalizeNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        var normalized = new System.Text.StringBuilder(value.Length);

        foreach (var character in value)
            if (char.IsLetterOrDigit(character))
                normalized.Append(char.ToUpperInvariant(character));

        return normalized.ToString();
    }

    /// <summary>
    /// Omie writes dates as dd/MM/yyyy and sends an empty string when there is none — a contract with no end date is
    /// the normal case, not a parsing failure. Anything it cannot read comes back null, so the caller shows "no
    /// validity" instead of a date that was never agreed.
    /// </summary>
    private static DateOnly? ParseDate(string? value)
        => DateOnly.TryParseExact(value?.Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None,
            out var date)
            ? date
            : null;
}
