namespace NuvTools.Payment.DTOs;

/// <summary>Money sent to a payee's account.</summary>
/// <param name="TransferId">The provider's identifier for the transfer.</param>
/// <param name="AmountMinor">How much was sent, in the currency's minor units.</param>
/// <param name="CurrencyCode">ISO 4217, upper case.</param>
public record PaymentTransferDTO(string TransferId, long AmountMinor, string CurrencyCode);
