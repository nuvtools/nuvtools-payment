namespace NuvTools.Payment.Omie.ApiClient.DTOs.Responses;

/// <summary>
/// RPS and batch status codes in Omie (<c>cStatusRps</c> / <c>cStatusLote</c>), as documented by the API. Both
/// fields use the same three digits.
/// </summary>
public static class OmieInvoiceRpsStatus
{
    /// <summary>Pending submission.</summary>
    public const string Pending = "001";

    /// <summary>Submitted, waiting for the city hall.</summary>
    public const string Sent = "002";

    /// <summary>Error — rejected.</summary>
    public const string Error = "003";

    /// <summary>Processed successfully.</summary>
    public const string Success = "004";

    /// <summary>Cancelled.</summary>
    public const string Cancelled = "005";
}
