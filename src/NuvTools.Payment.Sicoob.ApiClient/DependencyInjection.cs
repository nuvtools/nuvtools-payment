using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using NuvTools.Payment.Sicoob.ApiClient.Configuration;
using NuvTools.Payment.Sicoob.ApiClient.Contracts;

namespace NuvTools.Payment.Sicoob.ApiClient;

/// <summary>
/// Extensoes para registro do cliente Sicoob API no container de DI.
/// </summary>
public static class DependencyInjection
{
    private const string HttpClientName = "SicoobApi";

    /// <summary>
    /// Adiciona o cliente da API Sicoob ao container de servicos.
    /// </summary>
    public static IServiceCollection AddSicoobApiClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SicoobApiClientConfig>(
            configuration.GetSection(SicoobApiClientConfig.SectionName));

        services.AddHttpClient<ISicoobBankSlipApiClient, Services.SicoobBankSlipApiClient>(HttpClientName)
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
