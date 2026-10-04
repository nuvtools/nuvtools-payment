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

    /// <summary>
    /// The kind of connected account created for a payee: <c>express</c>, <c>standard</c> or
    /// <c>custom</c>. It decides who hosts the payee's dashboard and who answers for losses, which is
    /// the application's decision and not this client's.
    /// </summary>
    public string ConnectAccountType { get; set; } = "express";

    /// <summary>
    /// The capabilities requested for a connected account, by Stripe's own names —
    /// <c>transfers</c>, <c>card_payments</c>. Empty requests <c>transfers</c> alone, the least an
    /// account that is paid by transfer needs.
    /// </summary>
    /// <remarks>
    /// Stripe decides per country which combinations it accepts: Brazil, for one, refuses
    /// <c>transfers</c> without <c>card_payments</c>. An application that onboards payees there lists
    /// both; the default stays the minimum, so nobody is asked for more than their country requires.
    /// Left empty rather than defaulted to a list, because configuration appends to a list that
    /// already holds something.
    /// </remarks>
    public IList<string> ConnectCapabilities { get; } = [];

    /// <summary>
    /// The payment methods a customer may save on the hosted page, by Stripe's own names —
    /// <c>card</c>, <c>sepa_debit</c>. Empty leaves the choice to what the Stripe account has enabled
    /// for the currency of the request.
    /// </summary>
    /// <remarks>
    /// Named by an application that can only use some of them afterwards: one that charges
    /// off-session, or shows the saved method as a brand and four digits, lists <c>card</c>.
    /// </remarks>
    public IList<string> PaymentMethodTypes { get; } = [];

    /// <summary>How long one call to Stripe may take before it is abandoned.</summary>
    public int RequestTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// How many times the SDK retries a call it could not complete. Stripe's own client retries only
    /// what is safe to retry, and sends the idempotency key again when it does.
    /// </summary>
    public int MaxNetworkRetries { get; set; } = 2;
}
