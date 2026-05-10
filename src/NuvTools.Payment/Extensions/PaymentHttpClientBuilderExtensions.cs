using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

namespace NuvTools.Payment.Extensions;

/// <summary>
/// Shared HttpClient registration helpers for payment provider API clients.
/// </summary>
public static class PaymentHttpClientBuilderExtensions
{
    /// <summary>
    /// Registers a typed <see cref="HttpClient"/> for a payment provider API client
    /// with the standard resilience policy used across all NuvTools.Payment providers.
    /// </summary>
    /// <typeparam name="TInterface">The contract interface.</typeparam>
    /// <typeparam name="TImpl">The concrete implementation.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="name">The named HttpClient identifier.</param>
    /// <returns>The <see cref="IHttpClientBuilder"/> for further configuration.</returns>
    public static IHttpClientBuilder AddPaymentResilientHttpClient<TInterface, TImpl>(
        this IServiceCollection services,
        string name)
        where TInterface : class
        where TImpl : class, TInterface
    {
        var builder = services.AddHttpClient<TInterface, TImpl>(name);
        builder.AddStandardResilienceHandler(StandardPaymentResilience);
        return builder;
    }

    /// <summary>
    /// The canonical resilience policy applied to payment provider HttpClients:
    /// 2 retries with 1s delay, 30s per-attempt timeout, 60s circuit-breaker sampling,
    /// 90s total request timeout.
    /// </summary>
    public static readonly Action<HttpStandardResilienceOptions> StandardPaymentResilience = opts =>
    {
        opts.Retry.MaxRetryAttempts = 2;
        opts.Retry.Delay = TimeSpan.FromSeconds(1);
        opts.AttemptTimeout.Timeout = TimeSpan.FromSeconds(30);
        opts.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(60);
        opts.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(90);
    };
}
