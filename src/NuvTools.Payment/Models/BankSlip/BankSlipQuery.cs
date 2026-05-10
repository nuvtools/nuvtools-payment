namespace NuvTools.Payment.Models.BankSlip;

/// <summary>
/// Lookup parameters for a single bank slip. Same shape as <see cref="BankSlipReference"/>;
/// kept as a distinct type so query and identity-mutation operations are not interchangeable.
/// </summary>
public class BankSlipQuery
{
    public int ClientNumber { get; set; }

    public int ModalityCode { get; set; }

    public long OurNumber { get; set; }
}
