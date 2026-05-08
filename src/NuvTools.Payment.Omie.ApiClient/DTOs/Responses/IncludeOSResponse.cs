using System.Text.Json.Serialization;

namespace NuvTools.Payment.Omie.ApiClient.DTOs.Responses;

public class IncludeOSResponse
{
    [JsonPropertyName("cCodIntOS")]
    public string? OsIntegrationCode { get; set; }

    [JsonPropertyName("nCodOS")]
    public long OsCode { get; set; }

    [JsonPropertyName("cNumOS")]
    public string? OsNumber { get; set; }

    [JsonPropertyName("cCodStatus")]
    public string? StatusCode { get; set; }

    [JsonPropertyName("cDescStatus")]
    public string? StatusDescription { get; set; }
}
