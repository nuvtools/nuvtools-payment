using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NuvTools.Payment.Contracts;
using NuvTools.Payment.Extensions;
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

        services.AddPaymentResilientHttpClient<ISicoobBankSlipApiClient, Services.SicoobBankSlipApiClient>(HttpClientName);

        services.AddTransient<IBankSlipIssuanceClient>(sp => sp.GetRequiredService<ISicoobBankSlipApiClient>());

        return services;
    }
}
