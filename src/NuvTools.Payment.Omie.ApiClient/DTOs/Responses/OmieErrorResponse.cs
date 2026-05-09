using System.Text.Json.Serialization;

namespace NuvTools.Payment.Omie.ApiClient.DTOs.Responses;

/// <summary>
/// Standard error response from the Omie API.
/// </summary>
internal class OmieErrorResponse
{
    [JsonPropertyName("faultstring")]
    public string? FaultString { get; set; }

    [JsonPropertyName("faultcode")]
    public string? FaultCode { get; set; }
}
