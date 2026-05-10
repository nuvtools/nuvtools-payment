namespace NuvTools.Payment.Models.BankSlip;

/// <summary>
/// Provider-neutral request to issue a bank slip. Fields here are the common subset
/// supported across issuance providers — provider-specific options
/// (e.g. Sicoob's protest/negativacao codes) stay on the provider request type.
/// </summary>
/// <remarks>
/// Inherited <see cref="BankSlipBase.DueDate"/>, <see cref="BankSlipBase.IssueDate"/>,
/// and <see cref="BankSlipBase.Payer"/> MUST be populated for issuance — provider
/// mappers throw <see cref="ArgumentNullException"/> if any of these is null.
/// </remarks>
public class BankSlipCreateRequest : BankSlipBase
{
    public BankSlipChargeRule? Charge { get; set; }

    public BankSlipDiscount? Discount { get; set; }

    public int InstallmentNumber { get; set; }

    /// <summary>Whether the payer accepts the slip (Sicoob: aceite).</summary>
    public bool Accept { get; set; }
}
