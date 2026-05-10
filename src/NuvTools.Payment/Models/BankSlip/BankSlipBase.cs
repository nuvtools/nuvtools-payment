namespace NuvTools.Payment.Models.BankSlip;

/// <summary>
/// Common fields shared by bank slip read and write models. Nullability follows the
/// read-side (looser) — write-side subclasses document and validate the tighter
/// "must be populated for issuance" semantic at the mapper boundary.
/// </summary>
public abstract class BankSlipBase
{
    public BankSlipReference Reference { get; set; } = new();

    /// <summary>Customer-supplied document number (Sicoob: seuNumero).</summary>
    public string? YourNumber { get; set; }

    public DateOnly? DueDate { get; set; }

    public DateOnly? IssueDate { get; set; }

    public decimal Amount { get; set; }

    public BankSlipPayer? Payer { get; set; }
}
