namespace NuvTools.Payment.DTOs.Requests;

/// <summary>What one charge needs.</summary>
/// <param name="CustomerId">Who is charged.</param>
/// <param name="PaymentMethodId">
/// Which saved method; null for the customer's default, which is what a scheduled bill wants.
/// </param>
/// <param name="AmountMinor">
/// How much, in the currency's minor units — 4990 is BRL 49.90, 1000 is JPY 1,000. Providers count
/// money this way because a decimal cannot say what a currency's smallest coin is.
/// </param>
/// <param name="CurrencyCode">ISO 4217, in any case.</param>
/// <param name="Description">What the customer sees on their statement.</param>
/// <param name="IdempotencyKey">
/// The caller's own key. Sending the same key twice returns the first answer instead of charging
/// twice, which is what makes a retried job safe. It is the caller's because only the caller knows
/// what "the same charge" means — and a key that has already answered a failure will keep answering
/// it, so a retry after a refusal needs a key of its own.
/// </param>
/// <param name="TransferGroup">
/// Ties this charge to the transfers paid out of it, for providers that support it.
/// </param>
/// <param name="Metadata">Carried on the provider's object; the caller's own identifiers belong here.</param>
public record PaymentChargeRequest(
    string CustomerId,
    string? PaymentMethodId,
    long AmountMinor,
    string CurrencyCode,
    string? Description,
    string IdempotencyKey,
    string? TransferGroup = null,
    IDictionary<string, string>? Metadata = null);
