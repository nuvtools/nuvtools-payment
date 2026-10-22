namespace NuvTools.Payment.Enumerations;

/// <summary>
/// Where a subscription stands, in terms a caller can store and decide on.
/// </summary>
/// <remarks>The values are meant to be stored, so they are explicit and never renumbered.</remarks>
public enum PaymentSubscriptionStatusType
{
    /// <summary>Not one of the states below.</summary>
    Unknown = 0,

    /// <summary>Created, but the first payment has not settled.</summary>
    Incomplete = 1,

    /// <summary>The first payment never settled, and the subscription will not start.</summary>
    IncompleteExpired = 2,

    /// <summary>In a free trial.</summary>
    Trialing = 3,

    /// <summary>Paid and running.</summary>
    Active = 4,

    /// <summary>A renewal failed and the provider is retrying it.</summary>
    PastDue = 5,

    /// <summary>Ended.</summary>
    Canceled = 6,

    /// <summary>The provider stopped retrying a failed renewal.</summary>
    Unpaid = 7,

    /// <summary>Paused, typically at the end of a trial with no payment method.</summary>
    Paused = 8
}
