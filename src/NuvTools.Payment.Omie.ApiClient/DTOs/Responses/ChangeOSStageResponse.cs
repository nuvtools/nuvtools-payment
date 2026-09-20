using System.Text.Json.Serialization;

namespace NuvTools.Payment.Omie.ApiClient.DTOs.Responses;

/// <summary>Response of TrocarEtapaOS.</summary>
public class ChangeOSStageResponse : IOmieBusinessStatus
{
    [JsonPropertyName("cCodStatus")]
    public string? StatusCode { get; set; }

    [JsonPropertyName("cDescStatus")]
    public string? StatusDescription { get; set; }
}
