using NuvTools.Common.ResultWrapper;
using NuvTools.Payment.DTOs;
using NuvTools.Payment.DTOs.Requests;

namespace NuvTools.Payment.Contracts;

/// <summary>
/// Recurring billing run by the provider: starting a subscription, letting the customer manage it,
/// and reading where it stands.
/// </summary>
/// <remarks>
/// <para>
/// The provider owns the cycle — renewals, retries, invoices, proration — and reports changes by
/// webhook. A caller keeps a copy of what it needs to decide on access and refreshes it from
/// <see cref="GetSubscriptionAsync"/> when an event arrives, rather than trusting the order events
/// arrive in.
/// </para>
/// <para>
/// Both pages are hosted by the provider, so no card reaches the caller.
/// </para>
/// </remarks>
public interface IPaymentSubscriptionClient
{
    /// <summary>A provider-hosted page that starts a subscription, and the address to send the customer to.</summary>
    /// <remarks>
    /// The subscription is reported by a webhook rather than by the return address, which the customer
    /// may never reach.
    /// </remarks>
    /// <param name="request">Who pays, for which price, and where to return to.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    Task<IResult<string>> CreateCheckoutPageAsync(
        SubscriptionCheckoutRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// A provider-hosted page where the customer changes their payment method, reads their invoices and
    /// changes or cancels their subscriptions, and the address to send them to.
    /// </summary>
    /// <param name="customerId">Whose subscriptions.</param>
    /// <param name="returnUrl">Where the customer returns when they are done.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    Task<IResult<string>> CreateManagementPageAsync(
        string customerId,
        string returnUrl,
        CancellationToken cancellationToken = default);

    /// <summary>A subscription as the provider holds it now.</summary>
    /// <param name="subscriptionId">The provider's identifier for it.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    Task<IResult<PaymentSubscriptionDTO>> GetSubscriptionAsync(
        string subscriptionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets how many units of a price a subscription is charged for — the seats of a per-seat price —
    /// adding the price when the subscription does not carry it yet and taking it off at zero.
    /// </summary>
    /// <remarks>
    /// The quantity is absolute, never a difference, so a call repeated after a failure leaves the same
    /// count. The change is prorated over the period already running, and the subscription is returned as
    /// it stands afterwards.
    /// </remarks>
    /// <param name="subscriptionId">The provider's identifier for the subscription.</param>
    /// <param name="priceLookupKey">The lookup key of the price whose quantity is set.</param>
    /// <param name="quantity">The new quantity; zero takes the price off the subscription.</param>
    /// <param name="idempotencyKey">Makes a retried request apply once.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    Task<IResult<PaymentSubscriptionDTO>> SetItemQuantityAsync(
        string subscriptionId,
        string priceLookupKey,
        long quantity,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default);
}
