using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NuvTools.Payment.Contracts;
using NuvTools.Payment.Stripe.ApiClient.Configuration;
using NuvTools.Payment.Stripe.ApiClient.Services;
using Stripe;

namespace NuvTools.Payment.Stripe.ApiClient;

/// <summary>
/// Registers Stripe as the payment provider.
/// </summary>
public static class DependencyInjection
{
    private const string HttpClientName = "StripeApi";

    /// <summary>
    /// Binds the <c>Stripe</c> configuration section and registers Stripe behind the
    /// <c>NuvTools.Payment</c> contracts — the customer, payee account, charge, refund and webhook
    /// clients.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the only place a caller names Stripe: everything above it depends on
    /// <see cref="IPaymentCustomerClient"/> and its siblings, so changing provider is changing this
    /// call.
    /// </para>
    /// <para>
    /// The SDK's client is built over an <see cref="HttpClient"/> from the factory, so the host's own
    /// handlers and logging apply, and its retries are the SDK's: it knows which calls are safe to
    /// repeat and sends the idempotency key again when it repeats one.
    /// </para>
    /// </remarks>
    /// <param name="services">The container.</param>
    /// <param name="configuration">Where the <c>Stripe</c> section is read from.</param>
    public static IServiceCollection AddStripeApiClient(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration.GetSection(StripeApiClientConfig.SectionName);

        services.Configure<StripeApiClientConfig>(section);

        var config = section.Get<StripeApiClientConfig>() ?? new StripeApiClientConfig();

        services.AddHttpClient(HttpClientName,
            client => client.Timeout = TimeSpan.FromSeconds(config.RequestTimeoutSeconds));

        services.AddSingleton<IStripeClient>(provider =>
        {
            var httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient(HttpClientName);

            return new StripeClient(config.SecretKey, httpClient: new SystemNetHttpClient(httpClient, config.MaxNetworkRetries));
        });

        services.AddScoped<IPaymentCustomerClient, StripeCustomerApiClient>();
        services.AddScoped<IPayeeAccountClient, StripeConnectApiClient>();
        services.AddScoped<IPaymentChargeClient, StripeChargeApiClient>();
        services.AddScoped<IPaymentRefundClient, StripeRefundApiClient>();
        services.AddSingleton<IPaymentWebhookVerifier, StripeWebhookVerifier>();

        return services;
    }
}
