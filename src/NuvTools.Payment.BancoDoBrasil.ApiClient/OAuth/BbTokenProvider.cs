using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NuvTools.Payment.BancoDoBrasil.ApiClient.Configuration;
using NuvTools.Payment.BancoDoBrasil.ApiClient.DTOs.Responses;

namespace NuvTools.Payment.BancoDoBrasil.ApiClient.OAuth;

/// <summary>
/// Provedor de tokens OAuth2 (client_credentials) da API Banco do Brasil, com cache por escopo.
/// A API de pagamento nao anexa o Bearer por conta propria; este provedor alimenta o
/// <see cref="BbAuthHandler"/>, que o injeta em cada requisicao.
/// </summary>
public sealed class BbTokenProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<BancoDoBrasilApiClientConfig> options,
    ILogger<BbTokenProvider> logger) : IBbTokenProvider
{
    private readonly BancoDoBrasilApiClientConfig _config = options.Value;
    private readonly ConcurrentDictionary<string, CachedToken> _cache = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public async Task<string> GetAccessTokenAsync(string scope, CancellationToken cancellationToken = default)
    {
        if (TryGetValid(scope, out var cached))
            return cached;

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            if (TryGetValid(scope, out cached))
                return cached;

            logger.LogInformation("Token BB: solicitando (scope: {Scope})", scope);

            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_config.ClientId}:{_config.ClientSecret}"));

            using var httpClient = httpClientFactory.CreateClient(nameof(BbTokenProvider));
            using var request = new HttpRequestMessage(HttpMethod.Post, _config.AuthUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["scope"] = scope
            });

            using var response = await httpClient.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Falha ao gerar token BB ({Scope}): {StatusCode} - {Body}", scope, response.StatusCode, body);
                throw new InvalidOperationException($"Falha ao gerar token de acesso do Banco do Brasil: {response.StatusCode}.");
            }

            var token = JsonSerializer.Deserialize<BbAccessTokenResponse>(body, JsonOptions);

            if (string.IsNullOrWhiteSpace(token?.AccessToken))
                throw new InvalidOperationException("Falha ao gerar token de acesso do Banco do Brasil: accessToken nulo.");

            // Margem de 1 minuto sobre o expires_in retornado (padrao 600s quando ausente).
            var expiresIn = token.ExpiresIn > 0 ? token.ExpiresIn : 600;
            var expiry = DateTimeOffset.UtcNow.AddSeconds(expiresIn).AddMinutes(-1);
            _cache[scope] = new CachedToken(token.AccessToken!, expiry);

            logger.LogInformation("Token BB: obtido (expira em {ExpiresIn}s)", expiresIn);

            return token.AccessToken!;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void InvalidateToken(string scope) => _cache.TryRemove(scope, out _);

    private bool TryGetValid(string scope, out string token)
    {
        if (_cache.TryGetValue(scope, out var cached) && DateTimeOffset.UtcNow < cached.Expiry)
        {
            token = cached.Token;
            return true;
        }

        token = string.Empty;
        return false;
    }

    private readonly record struct CachedToken(string Token, DateTimeOffset Expiry);
}
