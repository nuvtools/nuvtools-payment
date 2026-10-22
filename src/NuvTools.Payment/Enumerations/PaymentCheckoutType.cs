namespace NuvTools.Payment.Enumerations;

/// <summary>What a provider-hosted checkout was opened for.</summary>
/// <remarks>
/// Carried on a completed checkout so a caller can ignore the ones that are not its own: two services
/// sharing one provider account both receive every completed checkout.
/// </remarks>
public enum PaymentCheckoutType
{
    /// <summary>Not one of the purposes below.</summary>
    Unknown = 0,

    /// <summary>A one-off payment.</summary>
    Payment = 1,

    /// <summary>Saving a payment method for later.</summary>
    Setup = 2,

    /// <summary>Starting a subscription.</summary>
    Subscription = 3
}
