namespace NuvTools.Payment.Models.BankSlip;

/// <summary>
/// Defines how interest and fine should be calculated when a bank slip is paid past due.
/// Provider mappers translate enum values to provider-specific codes
/// (e.g. Sicoob's tipoJurosMora / tipoMulta integers).
/// </summary>
public class BankSlipChargeRule
{
    public BankSlipChargeType InterestType { get; set; }

    public decimal InterestValue { get; set; }

    public BankSlipChargeType FineType { get; set; }

    public decimal FineValue { get; set; }
}

/// <summary>How a charge value should be interpreted.</summary>
public enum BankSlipChargeType
{
    None = 0,
    FixedValue = 1,
    PercentageOnNominalValue = 2,
    DailyValue = 3
}
