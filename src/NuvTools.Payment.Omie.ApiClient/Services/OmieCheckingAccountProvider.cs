using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using NuvTools.Common.ResultWrapper;
using NuvTools.Payment.Omie.ApiClient.DTOs.Responses;
using NuvTools.Payment.Omie.ApiClient.Resources;

namespace NuvTools.Payment.Omie.ApiClient.Services;

/// <summary>
/// Omie checking accounts (<c>geral/contacorrente/</c>) — the list an application offers so the user can pick where
/// the receivable of a service order lands (<c>nCodCC</c>, required on inclusion).
/// <para>
/// Blocked accounts are left out: Omie lists them but does not accept entries on them.
/// </para>
/// </summary>
public class OmieCheckingAccountProvider(OmieDirectApiClient omie, IMemoryCache cache)
{
    private const string CacheKey = "NuvTools:Omie:CheckingAccounts";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);
    private const int MaxPages = 20;
    private const int AccountsPerPage = 50;

    /// <summary>Usable checking accounts of the account's registration.</summary>
    public async Task<IResult<IReadOnlyList<OmieCheckingAccount>>> GetAsync(CancellationToken cancellationToken)
    {
        if (cache.TryGetValue<IReadOnlyList<OmieCheckingAccount>>(CacheKey, out var cached) && cached is not null)
            return Result<IReadOnlyList<OmieCheckingAccount>>.Success(cached);

        var accounts = new List<OmieCheckingAccount>();

        for (var page = 1; page <= MaxPages; page++)
        {
            var response = await omie.CallAsync<ListCheckingAccountsResponse>(
                omie.Options.EndpointCheckingAccount,
                "ListarContasCorrentes",
                new { pagina = page, registros_por_pagina = AccountsPerPage, apenas_importado_api = "N" },
                cancellationToken);

            if (!response.Succeeded)
            {
                if (OmieDirectApiClient.IsEndOfPages(response.Message)) break;
                return Result<IReadOnlyList<OmieCheckingAccount>>.Fail(response.Messages);
            }

            foreach (var account in response.Data?.Accounts ?? [])
            {
                if (account.Code <= 0 || string.Equals(account.IsBlocked, "S", StringComparison.OrdinalIgnoreCase))
                    continue;

                accounts.Add(new OmieCheckingAccount(account.Code, account.Description ?? account.Code.ToString(), account.AccountType));
            }

            if (response.Data is null || response.Data.TotalPages <= page) break;
        }

        if (accounts.Count == 0)
            return Result<IReadOnlyList<OmieCheckingAccount>>.Fail(Messages.OmieNoCheckingAccountAvailable);

        var list = (IReadOnlyList<OmieCheckingAccount>)accounts;
        cache.Set(CacheKey, list, CacheDuration);
        return Result<IReadOnlyList<OmieCheckingAccount>>.Success(list);
    }

    private sealed class ListCheckingAccountsResponse
    {
        [JsonPropertyName("total_de_paginas")] public int TotalPages { get; set; }
        [JsonPropertyName("ListarContasCorrentes")] public List<CheckingAccountRecord>? Accounts { get; set; }
    }

    private sealed class CheckingAccountRecord
    {
        [JsonPropertyName("nCodCC")] public long Code { get; set; }
        [JsonPropertyName("descricao")] public string? Description { get; set; }
        [JsonPropertyName("tipo_conta_corrente")] public string? AccountType { get; set; }
        [JsonPropertyName("bloqueado")] public string? IsBlocked { get; set; }
    }
}
