using NuvTools.Payment.Models.Common;

namespace NuvTools.Payment.Models.BankSlip;

/// <summary>
/// Payer (customer) information for a bank slip.
/// </summary>
public class BankSlipPayer
{
    public PartyIdentification Identification { get; set; } = new();

    public string? Name { get; set; }

    public Address? Address { get; set; }

    public string? Email { get; set; }
}
