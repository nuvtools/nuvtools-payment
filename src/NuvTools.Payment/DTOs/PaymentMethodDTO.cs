namespace NuvTools.Payment.DTOs;

/// <summary>A payment method as a screen shows it — never the number.</summary>
/// <param name="PaymentMethodId">The provider's identifier for it.</param>
/// <param name="Brand">The card network, when the method is a card.</param>
/// <param name="Last4">The last four digits, which is as much as anyone needs to recognise it.</param>
/// <param name="ExpiryMonth">The month the card expires, 1 to 12, when the method is a card.</param>
/// <param name="ExpiryYear">The four-digit year the card expires, when the method is a card.</param>
/// <param name="IsDefault">
/// Whether later charges are taken from it. Set when methods are listed; a call that reads only the
/// default leaves it <see langword="false"/>, because there it says nothing.
/// </param>
public record PaymentMethodDTO(
    string PaymentMethodId,
    string? Brand,
    string? Last4,
    int? ExpiryMonth = null,
    int? ExpiryYear = null,
    bool IsDefault = false);
