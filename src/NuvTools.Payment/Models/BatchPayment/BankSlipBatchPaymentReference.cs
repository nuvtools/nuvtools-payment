namespace NuvTools.Payment.Models.BatchPayment;

/// <summary>
/// Identifies a previously-created batch payment for status retrieval.
/// </summary>
public class BankSlipBatchPaymentReference
{
    public long PaymentId { get; set; }

    public string Agency { get; set; } = string.Empty;

    public string Account { get; set; } = string.Empty;

    public string AccountDigit { get; set; } = string.Empty;
}
