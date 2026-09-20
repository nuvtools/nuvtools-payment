namespace NuvTools.Payment.Enumerations;

/// <summary>
/// What became of one payment, in the few outcomes a caller can act on.
/// </summary>
/// <remarks>
/// Providers report far more states than this, and they all name them differently. These four are
/// the ones that change what the caller does next, so a provider client maps its own vocabulary onto
/// them and keeps the rest to itself.
/// </remarks>
public enum PaymentStatusType
{
    /// <summary>Taken, not settled: the provider has it and has not said yes or no yet.</summary>
    Pending = 1,

    /// <summary>The money arrived. The only outcome that clears a debt.</summary>
    Succeeded = 2,

    /// <summary>
    /// The customer has to finish it — a card asking for its owner to be present, typically. Not a
    /// refusal: charging again would be charging twice.
    /// </summary>
    RequiresAction = 3,

    /// <summary>Refused, and nothing was taken. Retrying needs a new attempt.</summary>
    Failed = 4
}
