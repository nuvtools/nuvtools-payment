namespace NuvTools.Payment.Models.BankSlip;

/// <summary>
/// Identifies a bank slip across operations. Field shapes follow the Sicoob model
/// (the only neutral-issuance provider) — other providers may ignore some fields.
/// </summary>
public class BankSlipReference
{
    public int ClientNumber { get; set; }

    public int ModalityCode { get; set; }

    public long OurNumber { get; set; }
}
