namespace NuvTools.Payment.Enumerations;

/// <summary>
/// Which endpoint a webhook arrived on, for providers that sign each one with its own secret.
/// </summary>
public enum PaymentWebhookEndpointType
{
    /// <summary>The platform's own endpoint: customers, payments and everything else.</summary>
    Platform = 1,

    /// <summary>
    /// The endpoint events about payee accounts are delivered to, when the provider is configured to
    /// keep them apart from the platform's own.
    /// </summary>
    Payee = 2
}
