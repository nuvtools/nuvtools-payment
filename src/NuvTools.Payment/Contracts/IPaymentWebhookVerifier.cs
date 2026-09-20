using NuvTools.Common.ResultWrapper;
using NuvTools.Payment.DTOs;
using NuvTools.Payment.Enumerations;

namespace NuvTools.Payment.Contracts;

/// <summary>
/// Checks that a webhook really came from the provider, and reads it.
/// </summary>
/// <remarks>
/// A webhook endpoint is public, so the signature is the whole of its authentication: an unverified
/// body is an anonymous stranger's, and acting on one would let anybody mark a bill paid.
/// </remarks>
public interface IPaymentWebhookVerifier
{
    /// <summary>The event, when the signature matches; a failure when it does not.</summary>
    /// <param name="payload">
    /// The request body exactly as received. Re-serializing it — even into an equivalent document —
    /// breaks the signature, so it has to be read as a string before anything parses it.
    /// </param>
    /// <param name="signature">The signature header the provider sent it with.</param>
    /// <param name="endpoint">Which endpoint it arrived on, for providers that sign each one separately.</param>
    IResult<PaymentEventDTO> Verify(
        string payload,
        string? signature,
        PaymentWebhookEndpointType endpoint = PaymentWebhookEndpointType.Platform);
}
