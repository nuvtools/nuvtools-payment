using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

        // Registrations the typed client does not cover, or covers without the fields a caller needs: clients
        // (only consulted by the very code one wants to discover), categories (the listing lacks whether the
        // account takes revenue and whether it is active), checking accounts, and single-order invoicing.
        // They share the same "Omie" configuration section and go through the raw call client.
        services.AddMemoryCache();
        services.AddHttpClient<Services.OmieDirectApiClient>();
        services.AddScoped<Services.OmieClientDirectoryProvider>();
        services.AddScoped<Services.OmieCategoryProvider>();
        services.AddScoped<Services.OmieCheckingAccountProvider>();
        services.AddScoped<Services.OmieServiceCatalogProvider>();
        services.AddScoped<Services.OmieContractProvider>();
        services.AddScoped<Services.OmieServiceOrderBillingProvider>();

        return services;
    }
}
