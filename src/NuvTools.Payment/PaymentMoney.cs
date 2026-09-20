namespace NuvTools.Payment;

/// <summary>
/// Amounts as a payment provider counts them: whole numbers of the currency's smallest unit.
/// </summary>
/// <remarks>
/// BRL 49.90 is 4990, JPY 1,000 is 1000, KWD 1.500 is 1500. The currency's minor units come from the
/// caller — ISO 4217 reference data — because this package does not carry a currency table.
/// </remarks>
public static class PaymentMoney
{
    /// <summary>
    /// The amount in minor units, rounded half away from zero at the currency's own place. Round once
    /// and on the total, never on a unit price: rounding twice is how a total stops matching its
    /// lines.
    /// </summary>
    /// <param name="amount">The amount to convert.</param>
    /// <param name="minorUnits">The currency's ISO 4217 places: 2 for BRL, 0 for JPY, 3 for KWD.</param>
    public static long ToMinorUnits(decimal amount, byte minorUnits) =>
        (long)decimal.Round(amount * Factor(minorUnits), 0, MidpointRounding.AwayFromZero);

    /// <summary>The inverse, for showing an amount the provider reported.</summary>
    /// <param name="amountMinor">The amount in minor units.</param>
    /// <param name="minorUnits">The currency's ISO 4217 places.</param>
    public static decimal FromMinorUnits(long amountMinor, byte minorUnits) => amountMinor / Factor(minorUnits);

    private static decimal Factor(byte minorUnits)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(minorUnits, (byte)4);

        decimal factor = 1;

        for (var index = 0; index < minorUnits; index++) factor *= 10;

        return factor;
    }
}
