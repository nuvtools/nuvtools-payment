using NuvTools.Common.ResultWrapper;
using NuvTools.Payment.Models.BankSlip;

namespace NuvTools.Payment.Contracts;

/// <summary>
/// Provider-neutral contract for bank slip (boleto) issuance — the receiver-side role
/// where slips are emitted for customers to pay. Implemented by Sicoob.
/// </summary>
/// <remarks>
/// Provider-specific extras (negativacao codes, protest days, etc.) are not exposed here.
/// Drop down to the provider-specific contract when those are required.
/// </remarks>
public interface IBankSlipIssuanceClient
{
    Task<IResult<BankSlip>> CreateAsync(BankSlipCreateRequest request, CancellationToken cancellationToken = default);

    Task<IResult<BankSlip>> GetAsync(BankSlipQuery query, CancellationToken cancellationToken = default);

    Task<IResult<IReadOnlyList<BankSlip>>> ListByPeriodAsync(BankSlipPeriodQuery query, CancellationToken cancellationToken = default);

    Task<IResult<BankSlipSecondCopy>> GetSecondCopyAsync(BankSlipQuery query, CancellationToken cancellationToken = default);

    Task<IResult> CancelAsync(BankSlipReference reference, CancellationToken cancellationToken = default);

    Task<IResult> ExtendDueDateAsync(BankSlipReference reference, DateOnly newDueDate, CancellationToken cancellationToken = default);

    Task<IResult> ChangeAmountAsync(BankSlipReference reference, decimal newAmount, CancellationToken cancellationToken = default);
}
