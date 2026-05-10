namespace NuvTools.Payment.Models.BatchPayment;

/// <summary>
/// Refund (devolução) recorded against a previously-paid batch item.
/// </summary>
public class BankSlipBatchPaymentRefund
{
    public int ReasonCode { get; set; }

    public string? ReasonDescription { get; set; }

    public decimal Amount { get; set; }

    public DateOnly? Date { get; set; }
}
