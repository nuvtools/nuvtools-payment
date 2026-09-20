using Microsoft.Extensions.Caching.Memory;
using NuvTools.Common.ResultWrapper;
using NuvTools.Payment.Omie.ApiClient.Contracts;
using NuvTools.Payment.Omie.ApiClient.DTOs.Responses;
using NuvTools.Payment.Omie.ApiClient.Resources;

namespace NuvTools.Payment.Omie.ApiClient.Services;

/// <summary>
/// Omie service catalog, read whole (<c>ListarCadastroServico</c>, paginated) and indexed by the business code
/// (<c>cCodigo</c>) — the code applications store in their own configuration. This is how the <c>nCodServico</c>,
/// the description and the unit price of a service are discovered: Omie exposes no search by <c>cCodigo</c>, so the
/// catalog is listed and searched in memory.
/// <para>
/// The result is cached for <see cref="CacheDuration"/> because Omie refuses repeated <c>ListarCadastroServico</c>
/// calls in a short window ("Consumo redundante detectado") — without the cache, opening a screen and generating a
/// service order right after would drop the second call.
/// </para>
/// </summary>
public class OmieServiceCatalogProvider(IOmieApiClient omie, IMemoryCache cache)
{
    private const int ServicesPerPage = 100;

    private const string CacheKey = "NuvTools:Omie:ServiceCatalog";

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Catalog indexed by <c>cCodigo</c>. With <paramref name="forceRefresh"/> the cache is ignored and rewritten
    /// with whatever Omie returns.
    /// </summary>
    public async Task<IResult<IReadOnlyDictionary<string, OmieServiceInfo>>> GetByCodeAsync(
        bool forceRefresh, CancellationToken cancellationToken)
    {
        if (!forceRefresh
            && cache.TryGetValue<IReadOnlyDictionary<string, OmieServiceInfo>>(CacheKey, out var cached)
            && cached is not null)
            return Result<IReadOnlyDictionary<string, OmieServiceInfo>>.Success(cached);

        var services = new Dictionary<string, OmieServiceInfo>();
        var page = 1;
        while (true)
        {
            var listResult = await omie.ListServiceRegistrationAsync(page, ServicesPerPage, cancellationToken);
            if (!listResult.Succeeded || listResult.Data is null)
                return Result<IReadOnlyDictionary<string, OmieServiceInfo>>.Fail(
                    string.Format(Messages.OmieServiceListFailedX, listResult.Message));

            foreach (var service in listResult.Data.Services ?? [])
            {
                var code = service.Header?.Code;
                if (string.IsNullOrWhiteSpace(code)) continue;

                services[code] = new OmieServiceInfo(
                    service.IntList?.ServiceCode ?? 0,
                    code,
                    service.Header?.Description,
                    service.Header?.UnitPrice ?? 0m);
            }

            if (listResult.Data.TotalPages <= page) break;
            page++;
        }

        var catalog = (IReadOnlyDictionary<string, OmieServiceInfo>)services;
        cache.Set(CacheKey, catalog, CacheDuration);
        return Result<IReadOnlyDictionary<string, OmieServiceInfo>>.Success(catalog);
    }

    /// <summary>
    /// Full registration of a service (<c>ConsultarCadastroServico</c>) — it carries what the listing does not and
    /// the service order inclusion requires: LC 116, municipal code and tax classification. Cached per service,
    /// because the same two or three services repeat across every client.
    /// </summary>
    public async Task<IResult<OmieServiceDetail>> GetServiceDetailAsync(long serviceCode, CancellationToken cancellationToken)
    {
        var cacheKey = $"NuvTools:Omie:ServiceDetail:{serviceCode}";

        if (cache.TryGetValue<OmieServiceDetail>(cacheKey, out var cached) && cached is not null)
            return Result<OmieServiceDetail>.Success(cached);

        var response = await omie.ConsultServiceRegistrationAsync(serviceCode, cancellationToken);

        if (!response.Succeeded || response.Data?.Header is null)
            return Result<OmieServiceDetail>.Fail(
                string.Format(Messages.OmieServiceDetailFailedXReasonX, serviceCode, response.Message));

        var header = response.Data.Header;

        var detail = new OmieServiceDetail(
            serviceCode,
            header.Code,
            header.Description,
            header.UnitPrice,
            header.Lc116Code,
            header.MunicipalServiceCode,
            response.Data.Taxes?.TaxClassification);

        cache.Set(cacheKey, detail, CacheDuration);
        return Result<OmieServiceDetail>.Success(detail);
    }

    /// <summary>
    /// The same catalog indexed by the internal identifier (<c>nCodServico</c>) — the way back, used when reading a
    /// service order from Omie, where items carry only the <c>nCodServico</c> and the caller needs the service name.
    /// </summary>
    public async Task<IResult<IReadOnlyDictionary<long, OmieServiceInfo>>> GetByServiceCodeAsync(
        CancellationToken cancellationToken)
    {
        var catalog = await GetByCodeAsync(false, cancellationToken);
        if (!catalog.Succeeded || catalog.Data is null)
            return Result<IReadOnlyDictionary<long, OmieServiceInfo>>.Fail(catalog.Messages);

        var byServiceCode = catalog.Data.Values
            .Where(s => s.ServiceCode > 0)
            .GroupBy(s => s.ServiceCode)
            .ToDictionary(g => g.Key, g => g.First());

        return Result<IReadOnlyDictionary<long, OmieServiceInfo>>.Success(byServiceCode);
    }
}
