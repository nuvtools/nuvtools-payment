namespace NuvTools.Payment.Enumerations;

/// <summary>Where an invoice stands, in terms a caller can store and show.</summary>
/// <remarks>The values are meant to be stored, so they are explicit and never renumbered.</remarks>
public enum PaymentInvoiceStatusType
{
    /// <summary>Not one of the states below.</summary>
    Unknown = 0,

    /// <summary>Still being written: it can change, and nobody owes it yet.</summary>
    Draft = 1,

    /// <summary>Issued and not paid — waiting for its charge, or after a charge failed.</summary>
    Open = 2,

    /// <summary>Paid in full.</summary>
    Paid = 3,

    /// <summary>Cancelled after it was issued; nobody owes it.</summary>
    Void = 4,

    /// <summary>Given up on: still owed, but the provider no longer tries to collect it.</summary>
    Uncollectible = 5
}
