using NuvTools.Common.ResultWrapper;
using NuvTools.Payment.DTOs;

namespace NuvTools.Payment.Contracts;

/// <summary>
/// The paying side: who is charged, and the payment method they saved.
/// </summary>
/// <remarks>
/// Implemented by a provider package. A caller depends on this and registers the provider in its
/// composition root, so which provider takes the money is a deployment decision rather than a
/// reference in its business code.
/// </remarks>
public interface IPaymentCustomerClient
{
    /// <summary>Creates a customer and returns the provider's identifier for it.</summary>
    /// <param name="email">Where the provider sends receipts.</param>
    /// <param name="name">The customer's name as it should appear on them.</param>
    /// <param name="metadata">Carried on the provider's object; the caller's own identifiers belong here.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    Task<IResult<string>> CreateCustomerAsync(
        string email,
        string? name,
        IDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// An attempt at saving a payment method for later, off-session use, for a caller that hosts its
    /// own form.
    /// </summary>
    /// <remarks>
    /// Hosting the form means the card and the provider's script reach the caller's own origin, which
    /// is a PCI surface and a content security policy exception.
    /// <see cref="CreateHostedPaymentMethodPageAsync"/> is the alternative that avoids both.
    /// </remarks>
    /// <param name="customerId">Whose payment method is being saved.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    Task<IResult<PaymentMethodIntentDTO>> CreatePaymentMethodIntentAsync(
        string customerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// A provider-hosted page for saving a payment method, and the address to send the customer to.
    /// </summary>
    /// <remarks>
    /// The saved method is reported by a webhook rather than by the return address, which the
    /// customer may never reach.
    /// </remarks>
    /// <param name="customerId">Whose payment method is being saved.</param>
    /// <param name="successUrl">Where the provider returns the customer once they have saved one.</param>
    /// <param name="cancelUrl">Where it returns them if they give up.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    Task<IResult<string>> CreateHostedPaymentMethodPageAsync(
        string customerId,
        string successUrl,
        string cancelUrl,
        CancellationToken cancellationToken = default);

    /// <summary>The customer's default payment method, or a failure when they have none.</summary>
    /// <param name="customerId">Whose payment method to read.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    Task<IResult<PaymentMethodDTO>> GetDefaultPaymentMethodAsync(
        string customerId,
        CancellationToken cancellationToken = default);

    /// <summary>Makes a payment method the one later charges are taken from.</summary>
    /// <param name="customerId">Whose default is being set.</param>
    /// <param name="paymentMethodId">The method to charge from now on.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    Task<IResult<PaymentMethodDTO>> SetDefaultPaymentMethodAsync(
        string customerId,
        string paymentMethodId,
        CancellationToken cancellationToken = default);
}
