using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NuvTools.Common.ResultWrapper;
using NuvTools.Payment.Omie.ApiClient.DTOs.Responses;
using NuvTools.Payment.Omie.ApiClient.Resources;

namespace NuvTools.Payment.Omie.ApiClient.Services;

/// <summary>
/// Resolves the category (<c>cCodCateg</c>) a service order carries in Omie. Omie requires the category on
/// inclusion and every account has its own chart of accounts — which is why it is discovered in the registration
/// instead of hardcoded: the categories are listed and the first of the configured preferences is picked by name.
/// <para>
/// Only accounts that can take a <b>revenue</b> entry are considered: <c>conta_receita = "S"</c>, active, not a
/// totalizer (totalizers are just groupings of the chart of accounts) and not a transfer account. Filtering by name
/// alone could land on an expense account with a similar name.
/// </para>
/// </summary>
public class OmieCategoryProvider(
    OmieDirectApiClient omie,
    IMemoryCache cache,
    ILogger<OmieCategoryProvider> logger)
{
    private const string CacheKey = "NuvTools:Omie:RevenueCategories";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    /// <summary>Safety stop for the pagination.</summary>
    private const int MaxPages = 50;

    /// <summary>
    /// Category code for the service order. <paramref name="configuredCode"/> wins when informed — whoever
    /// configured it knows which account they want. Without it, the <paramref name="preferences"/> are searched in
    /// order.
    /// </summary>
    public async Task<IResult<string>> ResolveCategoryCodeAsync(
        string? configuredCode, IReadOnlyList<string> preferences, CancellationToken cancellationToken)
    {
        // The argument goes by name: on Result<string> the positional overload is the message one, and Data would
        // come back null — the category would reach Omie blank, which it refuses without saying why.
        if (!string.IsNullOrWhiteSpace(configuredCode))
            return Result<string>.Success(data: configuredCode);

        if (preferences.Count == 0)
            return Result<string>.Fail(Messages.OmieCategoryNotConfigured);

        var categories = await GetRevenueCategoriesAsync(forceRefresh: false, cancellationToken);
        if (!categories.Succeeded || categories.Data is null)
            return Result<string>.Fail(categories.Messages);

        var match = Match(categories.Data, preferences);

        if (match is null)
            return Result<string>.Fail(string.Format(Messages.OmieNoCategoryMatchesXAvailableX,
                string.Join(", ", preferences),
                string.Join("; ", categories.Data.Select(c => $"{c.Code} {c.Description}"))));

        logger.LogInformation("Omie category resolved for service order: {Code} - {Description}", match.Code, match.Description);
        return Result<string>.Success(data: match.Code);
    }

    /// <summary>
    /// First preference that matches, in the order they were informed. Within a preference, the exact name beats
    /// the name that merely contains it — "Serviços Prestados" should prefer an account with that exact name over
    /// "Clientes - Serviços Prestados", but takes the second when the first does not exist.
    /// </summary>
    internal static OmieCategory? Match(IReadOnlyList<OmieCategory> categories, IReadOnlyList<string> preferences)
    {
        foreach (var preference in preferences)
        {
            var term = Normalize(preference);
            if (term.Length == 0) continue;

            var exact = categories.FirstOrDefault(c => Normalize(c.Description) == term);
            if (exact is not null) return exact;

            var partial = categories.FirstOrDefault(c => Normalize(c.Description).Contains(term, StringComparison.Ordinal));
            if (partial is not null) return partial;
        }

        return null;
    }

    /// <summary>
    /// Usable revenue categories of the chart of accounts — the list an application offers for selection.
    /// With <paramref name="forceRefresh"/> the cache is ignored and rewritten with whatever Omie returns, which is
    /// what a "reload from the ERP" action needs: without it the button would serve the cached list for the next
    /// 30 minutes and look like it did nothing.
    /// </summary>
    public async Task<IResult<IReadOnlyList<OmieCategory>>> GetRevenueCategoriesAsync(
        bool forceRefresh, CancellationToken cancellationToken)
    {
        if (!forceRefresh
            && cache.TryGetValue<IReadOnlyList<OmieCategory>>(CacheKey, out var cached)
            && cached is not null)
            return Result<IReadOnlyList<OmieCategory>>.Success(cached);

        var categories = new List<OmieCategory>();

        for (var page = 1; page <= MaxPages; page++)
        {
            var response = await omie.CallAsync<ListCategoriesResponse>(
                omie.Options.EndpointCategory,
                "ListarCategorias",
                new { pagina = page, registros_por_pagina = omie.Options.CategoriesPerPage },
                cancellationToken);

            if (!response.Succeeded)
            {
                if (OmieDirectApiClient.IsEndOfPages(response.Message)) break;
                return Result<IReadOnlyList<OmieCategory>>.Fail(response.Messages);
            }

            foreach (var category in response.Data?.Categories ?? [])
            {
                if (!IsUsableRevenueAccount(category)) continue;
                if (string.IsNullOrWhiteSpace(category.Code) || string.IsNullOrWhiteSpace(category.Description)) continue;

                categories.Add(new OmieCategory(category.Code, category.Description));
            }

            if (response.Data is null || response.Data.TotalPages <= page) break;
        }

        if (categories.Count == 0)
            return Result<IReadOnlyList<OmieCategory>>.Fail(Messages.OmieNoRevenueCategoryReturned);

        var list = (IReadOnlyList<OmieCategory>)categories;
        cache.Set(CacheKey, list, CacheDuration);
        return Result<IReadOnlyList<OmieCategory>>.Success(list);
    }

    private static bool IsUsableRevenueAccount(CategoryRecord category)
        => Yes(category.IsRevenueAccount)
            && !Yes(category.IsInactive)
            && !Yes(category.IsTotalizer)
            && !Yes(category.IsTransfer);

    private static bool Yes(string? flag) => string.Equals(flag, "S", StringComparison.OrdinalIgnoreCase);

    /// <summary>No accents, no case and no duplicate spaces — chart of accounts names vary in all three.</summary>
    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        var decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            builder.Append(char.ToUpperInvariant(c));
        }

        return string.Join(' ', builder.ToString()
            .Normalize(NormalizationForm.FormC)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    private sealed class ListCategoriesResponse
    {
        [JsonPropertyName("total_de_paginas")] public int TotalPages { get; set; }
        [JsonPropertyName("categoria_cadastro")] public List<CategoryRecord>? Categories { get; set; }
    }

    private sealed class CategoryRecord
    {
        [JsonPropertyName("codigo")] public string? Code { get; set; }
        [JsonPropertyName("descricao")] public string? Description { get; set; }
        [JsonPropertyName("conta_receita")] public string? IsRevenueAccount { get; set; }
        [JsonPropertyName("conta_inativa")] public string? IsInactive { get; set; }
        [JsonPropertyName("totalizadora")] public string? IsTotalizer { get; set; }
        [JsonPropertyName("transferencia")] public string? IsTransfer { get; set; }
    }
}
