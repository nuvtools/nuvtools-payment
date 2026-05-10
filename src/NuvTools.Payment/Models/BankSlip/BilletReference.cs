namespace NuvTools.Payment.Models.BankSlip;

/// <summary>
/// Identifies a billet for retrieval via <see cref="Contracts.IBankSlipBilletQuery"/>.
/// Field shapes follow Omie's billet model (the only neutral-billet provider) — exactly
/// one of <see cref="OmieNumericId"/> or <see cref="OmieIntegrationCode"/> must be set.
/// </summary>
public class BilletReference
{
    /// <summary>Omie's nCodTitulo — numeric, authoritative id assigned by Omie.</summary>
    public long? OmieNumericId { get; set; }

    /// <summary>Omie's cCodIntTitulo — caller-supplied integration code for the receivable.</summary>
    public string? OmieIntegrationCode { get; set; }
}
