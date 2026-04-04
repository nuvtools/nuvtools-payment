using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using NuvTools.Payment.Omie.ApiClient.Configuration;
using NuvTools.Payment.Omie.ApiClient.Contracts;

namespace NuvTools.Payment.Omie.ApiClient;

/// <summary>
/// Extensoes para registro do cliente Omie API no container de DI.
/// </summary>
public static class DependencyInjection
{
    private const string HttpClientName = "OmieApi";

    /// <summary>
    /// Adiciona o cliente da API Omie ao container de servicos.
    /// </summary>
    public static IServiceCollection AddOmieApiClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OmieApiClientConfig>(
            configuration.GetSection(OmieApiClientConfig.SectionName));

        services.AddHttpClient<IOmieApiClient, Services.OmieApiClient>(HttpClientName)
            .AddStandardResilienceHandler(ConfigureResilience);

        return services;
    }

    private static void ConfigureResilience(HttpStandardResilienceOptions opts)
    {
        opts.Retry.MaxRetryAttempts = 2;
        opts.Retry.Delay = TimeSpan.FromSeconds(1);
        opts.AttemptTimeout.Timeout = TimeSpan.FromSeconds(30);
        opts.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(60);
        opts.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(90);
    }
}
