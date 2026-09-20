using Microsoft.Extensions.Logging;
using NuvTools.Common.ResultWrapper;
using NuvTools.Payment.Contracts;
using NuvTools.Payment.DTOs;
using Stripe;

namespace NuvTools.Payment.Stripe.ApiClient.Services;

/// <inheritdoc cref="IPaymentCustomerClient" />
public class StripeCustomerApiClient(IStripeClient client, ILogger<StripeCustomerApiClient> logger) : IPaymentCustomerClient
{
    private readonly CustomerService _customers = new(client);
    private readonly SetupIntentService _setupIntents = new(client);
    private readonly global::Stripe.Checkout.SessionService _sessions = new(client);

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
        string customerId, string successUrl, string cancelUrl, CancellationToken cancellationToken = default) =>
        StripeCall.RunAsync(logger, nameof(CreateHostedPaymentMethodPageAsync), async () =>
        {
            var session = await _sessions.CreateAsync(new global::Stripe.Checkout.SessionCreateOptions
            {
                Mode = "setup",
                Customer = customerId,
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl
            }, cancellationToken: cancellationToken);

            return session.Url;
        });

    public Task<IResult<PaymentMethodDTO>> GetDefaultPaymentMethodAsync(string customerId, CancellationToken cancellationToken = default) =>
        StripeCall.RunAsync(logger, nameof(GetDefaultPaymentMethodAsync), async () =>
        {
            var customer = await _customers.GetAsync(
                customerId,
                new CustomerGetOptions { Expand = ["invoice_settings.default_payment_method"] },
                cancellationToken: cancellationToken);

            return Describe(customer.InvoiceSettings?.DefaultPaymentMethod);
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

    /// <summary>The brand and the last four, and nothing else: the rest is the customer's to keep.</summary>
    private static PaymentMethodDTO? Describe(PaymentMethod? paymentMethod) =>
        paymentMethod is null ? null : new PaymentMethodDTO(paymentMethod.Id, paymentMethod.Card?.Brand, paymentMethod.Card?.Last4);
}
