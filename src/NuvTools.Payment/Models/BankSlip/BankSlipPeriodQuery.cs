namespace NuvTools.Payment.Models.BankSlip;

/// <summary>
/// Filter for listing bank slips by payer and date range.
/// </summary>
public class BankSlipPeriodQuery
{
    /// <summary>Payer document (CPF/CNPJ digits only).</summary>
    public string PayerDocument { get; set; } = string.Empty;

    public int ClientNumber { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    /// <summary>Provider-specific status code to filter by.</summary>
    public int StatusCode { get; set; }
}
