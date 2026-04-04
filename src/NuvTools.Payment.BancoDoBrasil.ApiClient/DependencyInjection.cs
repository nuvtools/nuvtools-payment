using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using NuvTools.Payment.BancoDoBrasil.ApiClient.Configuration;
using NuvTools.Payment.BancoDoBrasil.ApiClient.Contracts;

namespace NuvTools.Payment.BancoDoBrasil.ApiClient;

/// <summary>
/// Extensoes para registro do cliente Banco do Brasil API no container de DI.
/// </summary>
public static class DependencyInjection
{
    private const string HttpClientName = "BancoDoBrasilApi";

    /// <summary>
    /// Adiciona o cliente da API Banco do Brasil ao container de servicos.
    /// </summary>
    public static IServiceCollection AddBancoDoBrasilApiClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<BancoDoBrasilApiClientConfig>(
            configuration.GetSection(BancoDoBrasilApiClientConfig.SectionName));

        services.AddHttpClient<IBbBankSlipPaymentApiClient, Services.BbBankSlipPaymentApiClient>(HttpClientName)
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
