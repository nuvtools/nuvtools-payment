using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NuvTools.Payment.BancoDoBrasil.ApiClient.Configuration;
using NuvTools.Payment.BancoDoBrasil.ApiClient.Contracts;
using NuvTools.Payment.Contracts;
using NuvTools.Payment.Extensions;

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

        services.AddPaymentResilientHttpClient<IBbBankSlipPaymentApiClient, Services.BbBankSlipPaymentApiClient>(HttpClientName);

        services.AddTransient<IBankSlipBatchPaymentClient>(sp => sp.GetRequiredService<IBbBankSlipPaymentApiClient>());

        return services;
    }
}
