namespace NuvTools.Payment.DTOs;

/// <summary>A payment method as a screen shows it — never the number.</summary>
/// <param name="PaymentMethodId">The provider's identifier for it.</param>
/// <param name="Brand">The card network, when the method is a card.</param>
/// <param name="Last4">The last four digits, which is as much as anyone needs to recognise it.</param>
public record PaymentMethodDTO(string PaymentMethodId, string? Brand, string? Last4);
