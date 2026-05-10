using NuvTools.Common.ResultWrapper;
using NuvTools.Payment.Models.BankSlip;

namespace NuvTools.Payment.Contracts;

/// <summary>
/// Narrow billet (boleto) retrieval surface — returns the printable identifiers
/// for an already-issued billet. Implemented by Omie.
/// </summary>
public interface IBankSlipBilletQuery
{
    Task<IResult<BankSlipBilletInfo>> GetBilletAsync(BilletReference reference, CancellationToken cancellationToken = default);
}
