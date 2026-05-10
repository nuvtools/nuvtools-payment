namespace NuvTools.Payment.Models.BatchPayment;

/// <summary>
/// Provider-neutral request to create a batch of bank slip payments
/// (the company-pays-suppliers role).
/// </summary>
public class BankSlipBatchPaymentRequest
{
    public long RequestNumber { get; set; }

    public int DebitAgency { get; set; }

    public long DebitAccount { get; set; }

    public string? DebitAccountDigit { get; set; }

    public IReadOnlyList<BankSlipBatchPaymentItem> Items { get; set; } = [];
}
