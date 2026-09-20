using NuvTools.Payment.Enumerations;

namespace NuvTools.Payment.DTOs;

/// <summary>One refund's outcome.</summary>
/// <param name="RefundId">
/// The provider's identifier, and what its webhooks name later — so it is what a caller stores to
/// recognise the settlement when it arrives.
/// </param>
/// <param name="AmountMinor">How much was given back, in the currency's minor units.</param>
/// <param name="CurrencyCode">ISO 4217, upper case.</param>
/// <param name="Status">What became of it.</param>
public record PaymentRefundDTO(string RefundId, long AmountMinor, string CurrencyCode, PaymentStatusType Status)
{
    /// <summary>The only status that means the money went back.</summary>
    public bool Succeeded => Status == PaymentStatusType.Succeeded;
}
