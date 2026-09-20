using Microsoft.Extensions.Logging;
using NuvTools.Common.ResultWrapper;
using NuvTools.Payment.Contracts;
using NuvTools.Payment.DTOs;
using NuvTools.Payment.DTOs.Requests;
using Stripe;

namespace NuvTools.Payment.Stripe.ApiClient.Services;

/// <inheritdoc cref="IPaymentChargeClient" />
/// <remarks>
/// Separate charges and transfers, deliberately: one charge can pay several payees, which a
/// destination charge — with its single destination — cannot express.
/// </remarks>
public class StripeChargeApiClient(IStripeClient client, ILogger<StripeChargeApiClient> logger) : IPaymentChargeClient
{
    private readonly PaymentIntentService _paymentIntents = new(client);
    private readonly TransferService _transfers = new(client);

    public Task<IResult<PaymentDTO>> ChargeAsync(
        PaymentChargeRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return StripeCall.RunAsync(logger, nameof(ChargeAsync), async () =>
        {
            var intent = await _paymentIntents.CreateAsync(new PaymentIntentCreateOptions
            {
                Customer = request.CustomerId,
                PaymentMethod = request.PaymentMethodId,
                Amount = request.AmountMinor,
                Currency = request.CurrencyCode.ToLowerInvariant(),
                Description = request.Description,
                TransferGroup = request.TransferGroup,
                Metadata = request.Metadata?.ToDictionary(e => e.Key, e => e.Value),

                // Nobody is at a keyboard when a month is billed. Stripe answers requires_action
                // instead of charging when the card insists on its owner being present.
                OffSession = true,
                Confirm = true,
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true,
                    AllowRedirects = "never"
                }
            },
            // The key travels with the request, so the same month charged twice by a retried job is
            // one charge. It is the caller's, because only the caller knows what "the same" means.
            new RequestOptions { IdempotencyKey = request.IdempotencyKey },
            cancellationToken);

            return new PaymentDTO(intent.Id, StripePaymentStatus.From(intent.Status), intent.ClientSecret);
        });
    }

    public Task<IResult<PaymentTransferDTO>> TransferAsync(
        PaymentTransferRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return StripeCall.RunAsync(logger, nameof(TransferAsync), async () =>
        {
            var transfer = await _transfers.CreateAsync(new TransferCreateOptions
            {
                Destination = request.DestinationAccountId,
                Amount = request.AmountMinor,
                Currency = request.CurrencyCode.ToLowerInvariant(),
                TransferGroup = request.TransferGroup,
                Metadata = request.Metadata?.ToDictionary(e => e.Key, e => e.Value)
            },
            new RequestOptions { IdempotencyKey = request.IdempotencyKey },
            cancellationToken);

            return new PaymentTransferDTO(transfer.Id, transfer.Amount, transfer.Currency.ToUpperInvariant());
        });
    }
}
