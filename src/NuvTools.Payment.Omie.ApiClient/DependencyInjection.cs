using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NuvTools.Payment.Contracts;
using NuvTools.Payment.Omie.ApiClient.Configuration;
using NuvTools.Payment.Omie.ApiClient.Contracts;

namespace NuvTools.Payment.Omie.ApiClient;

/// <summary>
/// Extensions to register the Omie API client in the DI container.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds the Omie API client to the service container.
    /// </summary>
    /// <remarks>
    /// Registered as a singleton because <see cref="Services.OmieApiClient"/> owns a
    /// static <see cref="HttpClient"/> (intentionally bypassing HttpClientFactory and
    /// the standard resilience pipeline — see the comment on _staticClient in the
    /// implementation for why).
    /// </remarks>
    public static IServiceCollection AddOmieApiClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OmieApiClientConfig>(
            configuration.GetSection(OmieApiClientConfig.SectionName));

        services.AddSingleton<IOmieApiClient, Services.OmieApiClient>();
        services.AddSingleton<IBankSlipBilletQuery>(sp => sp.GetRequiredService<IOmieApiClient>());

        return services;
    }
}
