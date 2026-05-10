namespace NuvTools.Payment.Models.BatchPayment;

/// <summary>
/// Outcome of a batch-payment creation request.
/// </summary>
public class BankSlipBatchPaymentResult
{
    public int RequestState { get; set; }

    public long RequestNumber { get; set; }

    public int ItemCount { get; set; }

    public decimal TotalAmount { get; set; }

    public IReadOnlyList<BankSlipBatchPaymentResultItem> Items { get; set; } = [];
}
