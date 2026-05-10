namespace NuvTools.Payment.Models.BankSlip;

/// <summary>
/// Provider-neutral bank slip projection — the common subset of fields exposed by
/// Sicoob (and similar issuers). Use the provider-specific contract for full fidelity.
/// </summary>
public class BankSlip : BankSlipBase
{
    public decimal RebateAmount { get; set; }

    public BankSlipStatus Status { get; set; }

    public string? DigitableLine { get; set; }

    public string? Barcode { get; set; }
}
