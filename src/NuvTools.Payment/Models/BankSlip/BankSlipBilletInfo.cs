namespace NuvTools.Payment.Models.BankSlip;

/// <summary>
/// Billet (boleto) information returned by a billet-query provider — the common subset
/// of fields between Omie's GerarBoleto and ObterBoleto endpoints.
/// </summary>
public class BankSlipBilletInfo
{
    public string? BilletLink { get; set; }

    public string? BilletNumber { get; set; }

    public string? Barcode { get; set; }

    public string? BankNumber { get; set; }

    public DateOnly? IssueDate { get; set; }

    public DateOnly? DueDate { get; set; }

    public decimal InterestPercentage { get; set; }

    public decimal FinePercentage { get; set; }
}
