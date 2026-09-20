using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NuvTools.Common.ResultWrapper;
using NuvTools.Payment.Omie.ApiClient.Resources;

namespace NuvTools.Payment.Omie.ApiClient.Services;

/// <summary>
/// Omie client registration indexed by document (CNPJ/CPF) — the way to discover the client code
/// (<c>codigo_cliente_omie</c>), and the only one: Omie does not look a client up by document, and the typed client
/// (<c>IOmieApiClient</c>) only consults by the very code one is trying to discover. The whole registration is
/// listed (<c>ListarClientes</c>, paginated) and searched by document.
/// <para>
/// The document is compared by digits only: in Omie it usually comes formatted ("12.345.678/0001-99") while
/// applications tend to store it clean — comparing as text would find nobody.
/// </para>
/// <para>
/// The listing is expensive, so it is cached for <see cref="CacheDuration"/>. A client just registered in Omie may,
/// for that long, still not be found.
/// </para>
/// </summary>
public class OmieClientDirectoryProvider(
    OmieDirectApiClient omie,
    IMemoryCache cache,
    ILogger<OmieClientDirectoryProvider> logger)
{
    private const string CacheKey = "NuvTools:Omie:ClientDirectory";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    /// <summary>Safety stop: the pagination loop must never run free if Omie returns something unexpected.</summary>
    private const int MaxPages = 200;

    /// <summary>Omie client code for the informed document.</summary>
    public async Task<IResult<long>> GetClientCodeAsync(string clientDocument, CancellationToken cancellationToken)
    {
        var directory = await GetDirectoryAsync(cancellationToken);
        if (!directory.Succeeded || directory.Data is null)
            return Result<long>.Fail(directory.Messages);

        var key = Digits(clientDocument);

        return directory.Data.TryGetValue(key, out var clientCode) && clientCode > 0
            ? Result<long>.Success(clientCode)
            : Result<long>.Fail(string.Format(Messages.OmieClientXNotFound, clientDocument));
    }

    /// <summary>The full registration indexed by the digits of the CNPJ/CPF.</summary>
    private async Task<IResult<IReadOnlyDictionary<string, long>>> GetDirectoryAsync(CancellationToken cancellationToken)
    {
        if (cache.TryGetValue<IReadOnlyDictionary<string, long>>(CacheKey, out var cached) && cached is not null)
            return Result<IReadOnlyDictionary<string, long>>.Success(cached);

        var clients = new Dictionary<string, long>();

        for (var page = 1; page <= MaxPages; page++)
        {
            var response = await omie.CallAsync<ListClientsResponse>(
                omie.Options.EndpointClient,
                "ListarClientes",
                new { pagina = page, registros_por_pagina = omie.Options.ClientsPerPage, apenas_importado_api = "N" },
                cancellationToken);

            if (!response.Succeeded)
            {
                // Omie ends pagination with an error, not with an empty page.
                if (OmieDirectApiClient.IsEndOfPages(response.Message)) break;
                return Result<IReadOnlyDictionary<string, long>>.Fail(response.Messages);
            }

            foreach (var client in response.Data?.Clients ?? [])
            {
                var document = Digits(client.Document);
                if (document.Length == 0 || client.ClientCode <= 0) continue;
                clients.TryAdd(document, client.ClientCode);
            }

            if (response.Data is null || response.Data.TotalPages <= page) break;
        }

        // An empty registration is not cached: it would be half an hour answering "client not found" to everyone,
        // hiding that the real problem is the listing having returned nobody.
        if (clients.Count == 0)
            return Result<IReadOnlyDictionary<string, long>>.Fail(Messages.OmieClientRegistrationEmpty);

        logger.LogInformation("Omie client directory loaded. Clients {Count}", clients.Count);

        var directory = (IReadOnlyDictionary<string, long>)clients;
        cache.Set(CacheKey, directory, CacheDuration);
        return Result<IReadOnlyDictionary<string, long>>.Success(directory);
    }

    private static string Digits(string? value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : new string([.. value.Where(char.IsDigit)]);

    private sealed class ListClientsResponse
    {
        [JsonPropertyName("total_de_paginas")] public int TotalPages { get; set; }
        [JsonPropertyName("clientes_cadastro")] public List<OmieClientRecord>? Clients { get; set; }
    }

    private sealed class OmieClientRecord
    {
        [JsonPropertyName("codigo_cliente_omie")] public long ClientCode { get; set; }
        [JsonPropertyName("cnpj_cpf")] public string? Document { get; set; }
    }
}
