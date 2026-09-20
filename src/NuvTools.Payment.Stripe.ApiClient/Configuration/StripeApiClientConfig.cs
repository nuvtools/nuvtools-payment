namespace NuvTools.Payment.Stripe.ApiClient.Configuration;

/// <summary>
/// Stripe client settings, bound from the <c>Stripe</c> configuration section.
/// </summary>
/// <remarks>
/// Every value here is a secret except the timeout. The secret key authenticates every call; the two
/// webhook secrets are what make an incoming webhook trustworthy, and a client with the wrong one
/// rejects genuine events rather than accepting forged ones.
/// </remarks>
public class StripeApiClientConfig
{
    public const string SectionName = "Stripe";

    /// <summary>The platform's secret key, <c>sk_test_…</c> or <c>sk_live_…</c>.</summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>Signing secret of the account webhook endpoint, <c>whsec_…</c>.</summary>
    public string WebhookSecret { get; set; } = string.Empty;

    /// <summary>
    /// Signing secret of the Connect webhook endpoint, when connected accounts are delivered to an
    /// endpoint of their own. Blank means both kinds arrive on one endpoint under
    /// <see cref="WebhookSecret"/>.
    /// </summary>
    public string ConnectWebhookSecret { get; set; } = string.Empty;

    /// <summary>How long one call to Stripe may take before it is abandoned.</summary>
    public int RequestTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// How many times the SDK retries a call it could not complete. Stripe's own client retries only
    /// what is safe to retry, and sends the idempotency key again when it does.
    /// </summary>
    public int MaxNetworkRetries { get; set; } = 2;
}
