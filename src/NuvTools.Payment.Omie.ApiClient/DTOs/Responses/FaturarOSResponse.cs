using System.Text.Json.Serialization;

namespace NuvTools.Payment.Omie.ApiClient.DTOs.Responses;

public class FaturarOSResponse
{
    [JsonPropertyName("cCodIntOS")]
    public string? CCodIntOS { get; set; }

    [JsonPropertyName("nCodOS")]
    public long NCodOS { get; set; }

    [JsonPropertyName("cCodStatus")]
    public string? CCodStatus { get; set; }

    [JsonPropertyName("cDescStatus")]
    public string? CDescStatus { get; set; }
}
