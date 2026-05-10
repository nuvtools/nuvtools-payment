namespace NuvTools.Payment.Models.BankSlip;

/// <summary>
/// Provider-neutral bank slip status. Provider mappers translate raw codes
/// (e.g. Sicoob's situacaoBoleto integers) to this enum.
/// </summary>
public enum BankSlipStatus
{
    Unknown = 0,
    Open = 1,
    Paid = 2,
    Settled = 3,
    Canceled = 4,
    Overdue = 5,
    Protested = 6
}
