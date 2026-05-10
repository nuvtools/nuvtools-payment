namespace NuvTools.Payment.Models.BankSlip;

/// <summary>
/// Discount condition applied to a bank slip when paid before <see cref="LimitDate"/>.
/// </summary>
public class BankSlipDiscount
{
    public BankSlipDiscountType Type { get; set; }

    public decimal Value { get; set; }

    public DateOnly? LimitDate { get; set; }
}

/// <summary>How a discount value should be interpreted.</summary>
public enum BankSlipDiscountType
{
    None = 0,
    FixedValue = 1,
    PercentageOnNominalValue = 2
}
