namespace NuvTools.Payment.Models.BatchPayment;

/// <summary>
/// Per-item outcome inside a batch-payment response.
/// </summary>
public class BankSlipBatchPaymentResultItem
{
    public int Sequence { get; set; }

    public string? Barcode { get; set; }

    /// <summary>Provider-specific state code for the item.</summary>
    public int State { get; set; }

    public string? StateDescription { get; set; }

    public decimal PaymentAmount { get; set; }

    public DateOnly? PaymentDate { get; set; }

    public DateOnly? DueDate { get; set; }

    public string? Description { get; set; }

    public string? Errors { get; set; }
}
