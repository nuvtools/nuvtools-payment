using Microsoft.Extensions.Logging;
using NuvTools.Common.ResultWrapper;
using NuvTools.Payment.Contracts;
using Microsoft.Extensions.Options;
using NuvTools.Payment.DTOs;
using NuvTools.Payment.DTOs.Requests;
using NuvTools.Payment.Stripe.ApiClient.Configuration;
using Stripe;

namespace NuvTools.Payment.Stripe.ApiClient.Services;

/// <inheritdoc cref="IPaymentCustomerClient" />
public class StripeCustomerApiClient(
    IStripeClient client,
    IOptions<StripeApiClientConfig> config,
    ILogger<StripeCustomerApiClient> logger) : IPaymentCustomerClient
{
    private readonly StripeApiClientConfig _config = config.Value;
    private readonly CustomerService _customers = new(client);
    private readonly SetupIntentService _setupIntents = new(client);
    private readonly CustomerPaymentMethodService _customerPaymentMethods = new(client);
    private readonly PaymentMethodService _paymentMethods = new(client);
    private readonly global::Stripe.Checkout.SessionService _sessions = new(client);

    public Task<IResult> DeleteCustomerAsync(string customerId, CancellationToken cancellationToken = default) =>
        StripeCall.RunAsync(logger, nameof(DeleteCustomerAsync), async () =>
        {
            await _customers.DeleteAsync(customerId, cancellationToken: cancellationToken);
        });

    public Task<IResult<string>> CreateCustomerAsync(
        string email, string? name, IDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default) =>
        StripeCall.RunAsync(logger, nameof(CreateCustomerAsync), async () =>
        {
            var customer = await _customers.CreateAsync(new CustomerCreateOptions
            {
                Email = email,
                Name = name,
                Metadata = metadata?.ToDictionary(e => e.Key, e => e.Value)
            }, cancellationToken: cancellationToken);

            return customer.Id;
        });

    public Task<IResult<PaymentMethodIntentDTO>> CreatePaymentMethodIntentAsync(
        string customerId, CancellationToken cancellationToken = default) =>
        StripeCall.RunAsync(logger, nameof(CreatePaymentMethodIntentAsync), async () =>
        {
            // Off-session: what is being saved is a card to charge later without the customer there,
            // which is the only kind of payment method a monthly bill can use.
            var intent = await _setupIntents.CreateAsync(new SetupIntentCreateOptions
            {
                Customer = customerId,
                Usage = "off_session",
                AutomaticPaymentMethods = new SetupIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true,
                    AllowRedirects = "never"
                }
            }, cancellationToken: cancellationToken);

            return new PaymentMethodIntentDTO(intent.Id, intent.ClientSecret);
        });

    public Task<IResult<string>> CreateHostedPaymentMethodPageAsync(
        HostedPaymentMethodPageRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return StripeCall.RunAsync(logger, nameof(CreateHostedPaymentMethodPageAsync), async () =>
        {
            var options = new global::Stripe.Checkout.SessionCreateOptions
            {
                Mode = "setup",
                Customer = request.CustomerId,

                // Nothing is priced on a setup session, but Stripe offers the methods that can pay
                // in a currency, so it is told which one the customer will be charged in. Without
                // it Stripe accepts the session only when the methods are named below.
                Currency = string.IsNullOrWhiteSpace(request.CurrencyCode)
                    ? null
                    : request.CurrencyCode.Trim().ToLowerInvariant(),
                SuccessUrl = request.SuccessUrl,
                CancelUrl = request.CancelUrl
            };

            // Only when the application narrowed them; otherwise what the Stripe account has enabled
            // for that currency is what the customer is offered.
            if (_config.PaymentMethodTypes.Count > 0)
                options.AllowedPaymentMethodTypes = [.. _config.PaymentMethodTypes];

            var session = await _sessions.CreateAsync(options, cancellationToken: cancellationToken);

            return session.Url;
        });
    }

    public Task<IResult<PaymentMethodDTO>> GetDefaultPaymentMethodAsync(string customerId, CancellationToken cancellationToken = default) =>
        StripeCall.RunAsync(logger, nameof(GetDefaultPaymentMethodAsync), async () =>
        {
            var customer = await _customers.GetAsync(
                customerId,
                new CustomerGetOptions { Expand = ["invoice_settings.default_payment_method"] },
                cancellationToken: cancellationToken);

            return Describe(customer.InvoiceSettings?.DefaultPaymentMethod);
        });

    public Task<IResult<IReadOnlyList<PaymentMethodDTO>>> ListPaymentMethodsAsync(
        string customerId, CancellationToken cancellationToken = default) =>
        StripeCall.RunAsync<IReadOnlyList<PaymentMethodDTO>>(logger, nameof(ListPaymentMethodsAsync), async () =>
        {
            var customer = await _customers.GetAsync(customerId, cancellationToken: cancellationToken);
            var defaultId = customer.InvoiceSettings?.DefaultPaymentMethodId;

            // Cards only: a monthly bill is charged off-session, which is what a saved card is for.
            var cards = await _customerPaymentMethods.ListAsync(customerId,
                new CustomerPaymentMethodListOptions { Type = "card", Limit = 100 },
                cancellationToken: cancellationToken);

            return [.. cards.Data.Select(e => Describe(e, e.Id == defaultId)!)];
        });

    public Task<IResult> RemovePaymentMethodAsync(string paymentMethodId, CancellationToken cancellationToken = default) =>
        StripeCall.RunAsync(logger, nameof(RemovePaymentMethodAsync), async () =>
        {
            await _paymentMethods.DetachAsync(paymentMethodId, cancellationToken: cancellationToken);
        });

    public Task<IResult<PaymentMethodDTO>> SetDefaultPaymentMethodAsync(
        string customerId, string paymentMethodId, CancellationToken cancellationToken = default) =>
        StripeCall.RunAsync(logger, nameof(SetDefaultPaymentMethodAsync), async () =>
        {
            var customer = await _customers.UpdateAsync(customerId, new CustomerUpdateOptions
            {
                InvoiceSettings = new CustomerInvoiceSettingsOptions { DefaultPaymentMethod = paymentMethodId },
                Expand = ["invoice_settings.default_payment_method"]
            }, cancellationToken: cancellationToken);

            return Describe(customer.InvoiceSettings?.DefaultPaymentMethod);
        });

    /// <summary>The brand, the last four and the expiry, and nothing else: the rest is the customer's to keep.</summary>
    private static PaymentMethodDTO? Describe(PaymentMethod? paymentMethod, bool isDefault = false) =>
        paymentMethod is null
            ? null
            : new PaymentMethodDTO(
                paymentMethod.Id,
                paymentMethod.Card?.Brand,
                paymentMethod.Card?.Last4,
                paymentMethod.Card is { ExpMonth: > 0 } card ? (int)card.ExpMonth : null,
                paymentMethod.Card is { ExpYear: > 0 } year ? (int)year.ExpYear : null,
                isDefault);
}
