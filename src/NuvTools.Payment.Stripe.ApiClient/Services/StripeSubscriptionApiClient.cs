using Microsoft.Extensions.Logging;
using NuvTools.Common.ResultWrapper;
using NuvTools.Payment.Contracts;
using NuvTools.Payment.DTOs;
using NuvTools.Payment.DTOs.Requests;
using Stripe;

namespace NuvTools.Payment.Stripe.ApiClient.Services;

/// <inheritdoc cref="IPaymentSubscriptionClient" />
/// <remarks>
/// Stripe Billing: Checkout in subscription mode, the customer portal, and the subscription itself.
/// Prices are found by lookup key, so the catalogue the caller keeps names prices by a stable key and
/// a price is replaced in the Dashboard by moving the key.
/// </remarks>
public class StripeSubscriptionApiClient(
    IStripeClient client,
    ILogger<StripeSubscriptionApiClient> logger) : IPaymentSubscriptionClient
{
    private readonly PriceService _prices = new(client);
    private readonly SubscriptionService _subscriptions = new(client);
    private readonly SubscriptionItemService _subscriptionItems = new(client);
    private readonly global::Stripe.Checkout.SessionService _checkoutSessions = new(client);
    private readonly global::Stripe.BillingPortal.SessionService _portalSessions = new(client);

    public async Task<IResult<string>> CreateCheckoutPageAsync(
        SubscriptionCheckoutRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var found = await StripeCall.RunAsync(logger, nameof(CreateCheckoutPageAsync), async () =>
            (await _prices.ListAsync(new PriceListOptions
            {
                LookupKeys = [request.PriceLookupKey],
                Active = true,
                Limit = 1
            }, cancellationToken: cancellationToken)).Data.FirstOrDefault());

        // A key that names no active price is the caller's catalogue and the Dashboard disagreeing; the
        // caller is told so rather than offered a checkout for nothing.
        if (found.ContainsNotFound)
            return Result<string>.FailNotFound($"No active price has the lookup key '{request.PriceLookupKey}'.");

        if (!found.Succeeded || found.Data is not { } price)
            return Result<string>.Fail(found.Messages);

        return await StripeCall.RunAsync(logger, nameof(CreateCheckoutPageAsync), async () =>
        {
            var metadata = request.Metadata?.ToDictionary(e => e.Key, e => e.Value);

            var session = await _checkoutSessions.CreateAsync(new global::Stripe.Checkout.SessionCreateOptions
            {
                Mode = "subscription",
                Customer = request.CustomerId,
                ClientReferenceId = request.ClientReferenceId,
                LineItems = [new global::Stripe.Checkout.SessionLineItemOptions { Price = price.Id, Quantity = 1 }],
                SuccessUrl = request.SuccessUrl,
                CancelUrl = request.CancelUrl,
                Metadata = metadata,

                // On the subscription too, because that is the object every later event is about —
                // the session is never seen again after it completes.
                SubscriptionData = new global::Stripe.Checkout.SessionSubscriptionDataOptions { Metadata = metadata }
            },
            string.IsNullOrWhiteSpace(request.IdempotencyKey) ? null : new RequestOptions { IdempotencyKey = request.IdempotencyKey },
            cancellationToken);

            return session.Url;
        });
    }

    public Task<IResult<string>> CreateManagementPageAsync(
        string customerId, string returnUrl, CancellationToken cancellationToken = default) =>
        StripeCall.RunAsync(logger, nameof(CreateManagementPageAsync), async () =>
        {
            var session = await _portalSessions.CreateAsync(new global::Stripe.BillingPortal.SessionCreateOptions
            {
                Customer = customerId,
                ReturnUrl = returnUrl
            }, cancellationToken: cancellationToken);

            return session.Url;
        });

    public Task<IResult<PaymentSubscriptionDTO>> GetSubscriptionAsync(
        string subscriptionId, CancellationToken cancellationToken = default) =>
        StripeCall.RunAsync(logger, nameof(GetSubscriptionAsync), async () =>
        {
            var subscription = await _subscriptions.GetAsync(subscriptionId, cancellationToken: cancellationToken);

            return Describe(subscription);
        });

    public async Task<IResult<PaymentSubscriptionDTO>> SetItemQuantityAsync(
        string subscriptionId, string priceLookupKey, long quantity,
        string? idempotencyKey = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subscriptionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(priceLookupKey);
        ArgumentOutOfRangeException.ThrowIfNegative(quantity);

        var read = await StripeCall.RunAsync(logger, nameof(SetItemQuantityAsync), async () =>
            await _subscriptions.GetAsync(subscriptionId, cancellationToken: cancellationToken));

        if (!read.Succeeded || read.Data is not { } subscription) return Result<PaymentSubscriptionDTO>.Fail(read.Messages);

        var item = subscription.Items?.Data?.FirstOrDefault(e => e.Price?.LookupKey == priceLookupKey);

        if (item is null && quantity == 0) return Result<PaymentSubscriptionDTO>.Success(Describe(subscription), string.Empty);

        if (item is not null && item.Quantity == quantity) return Result<PaymentSubscriptionDTO>.Success(Describe(subscription), string.Empty);

        var options = string.IsNullOrWhiteSpace(idempotencyKey) ? null : new RequestOptions { IdempotencyKey = idempotencyKey };

        IResult changed;

        if (item is null)
        {
            var found = await StripeCall.RunAsync(logger, nameof(SetItemQuantityAsync), async () =>
                (await _prices.ListAsync(new PriceListOptions
                {
                    LookupKeys = [priceLookupKey],
                    Active = true,
                    Limit = 1
                }, cancellationToken: cancellationToken)).Data.FirstOrDefault());

            if (found.ContainsNotFound)
                return Result<PaymentSubscriptionDTO>.FailNotFound($"No active price has the lookup key '{priceLookupKey}'.");

            if (!found.Succeeded || found.Data is not { } price) return Result<PaymentSubscriptionDTO>.Fail(found.Messages);

            changed = await StripeCall.RunAsync(logger, nameof(SetItemQuantityAsync), () =>
                _subscriptionItems.CreateAsync(new SubscriptionItemCreateOptions
                {
                    Subscription = subscriptionId,
                    Price = price.Id,
                    Quantity = quantity,
                    ProrationBehavior = "create_prorations"
                }, options, cancellationToken));
        }
        else if (quantity == 0)
        {
            changed = await StripeCall.RunAsync(logger, nameof(SetItemQuantityAsync), () =>
                _subscriptionItems.DeleteAsync(item.Id, new SubscriptionItemDeleteOptions
                {
                    ProrationBehavior = "create_prorations"
                }, options, cancellationToken));
        }
        else
        {
            changed = await StripeCall.RunAsync(logger, nameof(SetItemQuantityAsync), () =>
                _subscriptionItems.UpdateAsync(item.Id, new SubscriptionItemUpdateOptions
                {
                    Quantity = quantity,
                    ProrationBehavior = "create_prorations"
                }, options, cancellationToken));
        }

        if (!changed.Succeeded) return Result<PaymentSubscriptionDTO>.Fail(changed.Messages);

        return await GetSubscriptionAsync(subscriptionId, cancellationToken);
    }

    /// <remarks>
    /// The period end is read from the subscription's item: Stripe moved it there from the
    /// subscription in API version 2025-03-31, and every item of a subscription shares its period.
    /// </remarks>
    private static PaymentSubscriptionDTO Describe(Subscription subscription)
    {
        var item = subscription.Items?.Data?.FirstOrDefault();

        return new PaymentSubscriptionDTO(
            subscription.Id,
            subscription.CustomerId,
            StripeSubscriptionStatus.From(subscription.Status),
            subscription.Status,
            item?.Price?.LookupKey,
            item is null ? null : new DateTimeOffset(DateTime.SpecifyKind(item.CurrentPeriodEnd, DateTimeKind.Utc)),
            subscription.CancelAtPeriodEnd,
            subscription.Metadata ?? new Dictionary<string, string>(),
            [.. subscription.Items?.Data?.Select(e => new PaymentSubscriptionItemDTO(e.Price?.LookupKey, e.Quantity)) ?? []]);
    }
}
