namespace NuvTools.Payment.Omie.ApiClient.DTOs.Responses;

/// <summary>
/// The service invoice (NFS-e) of a service order and the progress of the RPS it came from. Issuing is a process,
/// not an instant: Omie transmits the RPS and the city hall authorizes it later, so there is a window in which the
/// invoice exists as a request and has no number yet.
/// </summary>
/// <param name="Number">Number of the authorized NFS-e (<c>nNfse</c>). Empty until the city hall authorizes it.</param>
/// <param name="RpsStatus">RPS status (<c>cStatusRps</c>) — see <see cref="OmieInvoiceRpsStatus"/>.</param>
/// <param name="BatchStatus">Submission batch status (<c>cStatusLote</c>), using the same codes as the RPS.</param>
/// <param name="VerificationCode">NFS-e verification code (<c>cCodVerif</c>).</param>
/// <param name="Url">Link to the invoice on the city hall site (<c>cUrlNfse</c>).</param>
/// <param name="Message">City hall/Omie messages about the RPS, concatenated — the reason, when it was rejected.</param>
public record OmieServiceInvoiceStatus(
    string? Number,
    string? RpsStatus,
    string? BatchStatus,
    string? VerificationCode,
    string? Url,
    string? Message)
{
    /// <summary>The invoice is authorized: it has a number, which the city hall only grants at the end.</summary>
    public bool IsIssued => !string.IsNullOrWhiteSpace(Number);

    /// <summary>The city hall (or the batch) rejected the RPS. Without a registration fix there is no invoice.</summary>
    public bool IsRejected
        => RpsStatus == OmieInvoiceRpsStatus.Error || BatchStatus == OmieInvoiceRpsStatus.Error;

    /// <summary>The RPS was cancelled — no invoice will come out of it.</summary>
    public bool IsCancelled
        => RpsStatus == OmieInvoiceRpsStatus.Cancelled || BatchStatus == OmieInvoiceRpsStatus.Cancelled;
}
