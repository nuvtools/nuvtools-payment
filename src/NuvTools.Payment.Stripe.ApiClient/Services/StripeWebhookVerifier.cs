using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NuvTools.Common.ResultWrapper;
using NuvTools.Payment.Contracts;
using NuvTools.Payment.DTOs;
using NuvTools.Payment.Enumerations;
using NuvTools.Payment.Stripe.ApiClient.Configuration;
using Stripe;

namespace NuvTools.Payment.Stripe.ApiClient.Services;

/// <inheritdoc cref="IPaymentWebhookVerifier" />
public class StripeWebhookVerifier(
    IOptions<StripeApiClientConfig> options,
    ILogger<StripeWebhookVerifier> logger) : IPaymentWebhookVerifier
{
    private readonly StripeApiClientConfig _config = options.Value;

    public IResult<PaymentEventDTO> Verify(
        string payload, string? signature, PaymentWebhookEndpointType endpoint = PaymentWebhookEndpointType.Platform)
    {
        var secret = endpoint == PaymentWebhookEndpointType.Payee && !string.IsNullOrWhiteSpace(_config.ConnectWebhookSecret)
            ? _config.ConnectWebhookSecret
            : _config.WebhookSecret;

        if (string.IsNullOrWhiteSpace(secret))
            return Result<PaymentEventDTO>.Fail("No Stripe webhook signing secret is configured.");

        if (string.IsNullOrWhiteSpace(signature))
            return Result<PaymentEventDTO>.Fail("The request carries no Stripe signature.");

        try
        {
            // throwOnApiVersionMismatch: false — Stripe sends events in the version the endpoint was
            // created with, and an SDK upgrade must not stop a live endpoint accepting them.
            var stripeEvent = EventUtility.ConstructEvent(payload, signature, secret, throwOnApiVersionMismatch: false);

            return Result<PaymentEventDTO>.Success(StripeEventReader.Read(stripeEvent, payload));
        }
        catch (StripeException exception)
        {
            // Logged as a warning and answered as a refusal: an endpoint anybody can post to will be
            // posted to, and a failed signature is the system working.
            logger.LogWarning(exception, "A webhook was rejected: its signature did not verify.");

            return Result<PaymentEventDTO>.Fail("The Stripe signature did not verify.");
        }
    }
}
