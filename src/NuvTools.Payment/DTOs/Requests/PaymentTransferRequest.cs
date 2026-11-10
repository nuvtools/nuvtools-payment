namespace NuvTools.Payment.DTOs.Requests;

/// <summary>What one transfer to a payee needs.</summary>
/// <param name="DestinationAccountId">The payee's account at the provider.</param>
/// <param name="AmountMinor">How much, in the currency's minor units — what the payee keeps, after any fee.</param>
/// <param name="CurrencyCode">ISO 4217, in any case.</param>
/// <param name="IdempotencyKey">
/// The caller's own key. A payee paid twice is a mistake nobody can take back, so this is the one
/// argument that must never be reused across two different transfers.
/// </param>
/// <param name="TransferGroup">Ties this transfer to the charges it is paid out of.</param>
/// <param name="Metadata">Carried on the provider's object.</param>
/// <param name="SourceChargeId">
/// The charge the money comes from. The transfer is then paid out of that charge once it settles,
/// rather than out of the platform's available balance, which may not hold it yet.
/// </param>
public record PaymentTransferRequest(
    string DestinationAccountId,
    long AmountMinor,
    string CurrencyCode,
    string IdempotencyKey,
    string? TransferGroup = null,
    IDictionary<string, string>? Metadata = null,
    string? SourceChargeId = null);
