using System.Text.Json.Serialization;

namespace NuvTools.Payment.Omie.ApiClient.DTOs.Requests;

/// <summary>
/// Parameter for TrocarEtapaOS — moves a Service Order to another Kanban stage.
/// Send exactly one identifier (nCodOS preferred) plus the target stage code.
/// </summary>
public class ChangeOSStageParam
{
    [JsonPropertyName("cCodIntOS")]
    public string? OsIntegrationCode { get; set; }

    [JsonPropertyName("nCodOS")]
    public long? OsCode { get; set; }

    [JsonPropertyName("cEtapa")]
    public required string Stage { get; set; }
}
