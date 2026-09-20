using NuvTools.Payment.Enumerations;

namespace NuvTools.Payment.DTOs;

/// <summary>One payment's outcome.</summary>
/// <param name="PaymentId">
/// The provider's identifier, and what its webhooks name later — so it is what a caller stores to
/// recognise the settlement when it arrives.
/// </param>
/// <param name="Status">What became of it.</param>
/// <param name="ClientSecret">
/// Only useful when the payment needs the customer back to finish it; null otherwise.
/// </param>
public record PaymentDTO(string PaymentId, PaymentStatusType Status, string? ClientSecret)
{
    /// <summary>The only status that means the money arrived.</summary>
    public bool Succeeded => Status == PaymentStatusType.Succeeded;
}
