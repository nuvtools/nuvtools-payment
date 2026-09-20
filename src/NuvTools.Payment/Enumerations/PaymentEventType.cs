namespace NuvTools.Payment.Enumerations;

/// <summary>
/// The kinds of webhook a payment provider sends that a caller is expected to act on.
/// </summary>
/// <remarks>
/// A provider client maps its own event names onto these. Anything it does not recognise arrives as
/// <see cref="Unknown"/> rather than being dropped, because a caller still has to record that a
/// delivery happened — an endpoint that fails on an event it has no use for makes the provider retry
/// it for days.
/// </remarks>
public enum PaymentEventType
{
    /// <summary>Not one of the events below. Record it and ignore it.</summary>
    Unknown = 0,

    /// <summary>What the provider allows a payee to do has changed — being paid out, most of all.</summary>
    PayeeAccountUpdated = 1,

    /// <summary>A customer saved a payment method, and it can now be charged off-session.</summary>
    PaymentMethodSaved = 2,

    /// <summary>A payment settled.</summary>
    PaymentSucceeded = 3,

    /// <summary>A payment was refused.</summary>
    PaymentFailed = 4
}
