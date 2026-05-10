namespace NuvTools.Payment.Models.BatchPayment;

/// <summary>
/// Current status of a previously-created batch payment, including any refunds.
/// </summary>
public class BankSlipBatchPaymentStatus
{
    public int RequestState { get; set; }

    public long RequestNumber { get; set; }

    public int ItemCount { get; set; }

    public decimal TotalAmount { get; set; }

    public IReadOnlyList<BankSlipBatchPaymentResultItem> Items { get; set; } = [];

    public IReadOnlyList<BankSlipBatchPaymentRefund> Refunds { get; set; } = [];
}
