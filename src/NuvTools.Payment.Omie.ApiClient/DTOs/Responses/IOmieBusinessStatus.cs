namespace NuvTools.Payment.Omie.ApiClient.DTOs.Responses;

/// <summary>
/// Marker for Omie response DTOs that carry a business-level status code in the body.
/// Omie returns HTTP 200 even when the operation didn't succeed at the business level —
/// the body's status code is the real outcome.
/// </summary>
public interface IOmieBusinessStatus
{
    string? StatusCode { get; }

    string? StatusDescription { get; }
}
