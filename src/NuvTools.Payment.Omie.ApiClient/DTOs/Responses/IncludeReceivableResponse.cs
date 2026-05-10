using System.Text.Json.Serialization;

namespace NuvTools.Payment.Omie.ApiClient.DTOs.Responses;

public class IncludeReceivableResponse : IOmieBusinessStatus
{
    [JsonPropertyName("codigo_lancamento_omie")]
    public long OmieEntryCode { get; set; }

    [JsonPropertyName("codigo_lancamento_integracao")]
    public string? IntegrationEntryCode { get; set; }

    [JsonPropertyName("codigo_status")]
    public string? StatusCode { get; set; }

    [JsonPropertyName("descricao_status")]
    public string? StatusDescription { get; set; }
}
