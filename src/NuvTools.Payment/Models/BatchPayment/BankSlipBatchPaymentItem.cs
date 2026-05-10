namespace NuvTools.Payment.Models.BatchPayment;

/// <summary>
/// One bank slip in a batch-payment request.
/// </summary>
public class BankSlipBatchPaymentItem
{
    public int Sequence { get; set; }

    public string Barcode { get; set; } = string.Empty;

    public DateOnly DueDate { get; set; }

    public DateOnly PaymentDate { get; set; }

    public decimal PaymentAmount { get; set; }

    public decimal NominalAmount { get; set; }

    public string? Description { get; set; }
}
