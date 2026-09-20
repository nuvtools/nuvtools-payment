using System.Text.Json.Serialization;

namespace NuvTools.Payment.Omie.ApiClient.DTOs.Responses;

/// <summary>
/// Response of ListarEtapasFaturamento (servicos/osetapas/) — the Kanban stage catalog. Used to discover the
/// early stage to use on create and the billing/invoicing stage(s) that mark an OS as no longer editable.
/// VERIFY (Omie test app): confirm the list key ("cadastros") and the stage field names ("cCodigo"/"cDescricao").
/// </summary>
public class ListBillingStagesResponse : IOmieBusinessStatus
{
    [JsonPropertyName("cadastros")]
    public BillingStage[]? Stages { get; set; }

    [JsonPropertyName("cCodStatus")]
    public string? StatusCode { get; set; }

    [JsonPropertyName("cDescStatus")]
    public string? StatusDescription { get; set; }
}

public class BillingStage
{
    [JsonPropertyName("cCodigo")]
    public string? Code { get; set; }

    [JsonPropertyName("cDescricao")]
    public string? Description { get; set; }
}
