namespace NuvTools.Payment.Models.BankSlip;

/// <summary>
/// Reissued (second-copy) bank slip with the printable PDF payload.
/// </summary>
public class BankSlipSecondCopy
{
    public BankSlipReference Reference { get; set; } = new();

    public string? DigitableLine { get; set; }

    public string? Barcode { get; set; }

    /// <summary>Base64-encoded PDF of the slip.</summary>
    public string? PdfBase64 { get; set; }
}
