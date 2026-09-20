using NuvTools.Common.ResultWrapper;
using NuvTools.Payment.DTOs;
using NuvTools.Payment.DTOs.Requests;

namespace NuvTools.Payment.Contracts;

/// <summary>
/// Moving money: charging a customer, and paying a payee.
/// </summary>
/// <remarks>
/// The two are separate calls on purpose. One charge can pay several payees, and a payee is paid for
/// what was billed rather than for what each customer happened to pay — a customer who has not paid
/// is a debt the platform carries, while a payee paid twice is a mistake nobody can take back.
/// </remarks>
public interface IPaymentChargeClient
{
    /// <summary>
    /// Charges a saved payment method off-session — nobody is at a keyboard when a month is billed —
    /// and returns what the provider made of it.
    /// </summary>
    /// <param name="request">Who is charged, how much, and the key that makes a retry safe.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    Task<IResult<PaymentDTO>> ChargeAsync(
        PaymentChargeRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Sends part of what was charged to a payee's account.</summary>
    /// <param name="request">Who is paid, how much, and the key that makes a retry safe.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    Task<IResult<PaymentTransferDTO>> TransferAsync(
        PaymentTransferRequest request,
        CancellationToken cancellationToken = default);
}
