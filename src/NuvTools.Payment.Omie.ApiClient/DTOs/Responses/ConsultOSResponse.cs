using System.Text.Json.Serialization;

namespace NuvTools.Payment.Omie.ApiClient.DTOs.Responses;

/// <summary>
/// Response of ConsultarOS — the current state of a Service Order. Used to resolve OS stage before an
/// upsert: the caller compares <see cref="ConsultOSCabecalho.Stage"/> (cEtapa) against the stages returned
/// by ListarEtapasFaturamento to decide create / update / blocked-because-invoiced.
/// </summary>
public class ConsultOSResponse : IOmieBusinessStatus
{
    [JsonPropertyName("cabecalho")]
    public ConsultOSCabecalho? Header { get; set; }

    [JsonPropertyName("ServicosPrestados")]
    public ConsultOSProvidedService[]? ProvidedServices { get; set; }

    // ConsultarOS returns the OS body on success; a missing OS comes back as an HTTP fault (surfaced as a
    // failed IResult), so these status fields are usually absent on success.
    [JsonPropertyName("cCodStatus")]
    public string? StatusCode { get; set; }

    [JsonPropertyName("cDescStatus")]
    public string? StatusDescription { get; set; }
}

public class ConsultOSCabecalho
{
    [JsonPropertyName("nCodOS")]
    public long OsCode { get; set; }

    [JsonPropertyName("cCodIntOS")]
    public string? OsIntegrationCode { get; set; }

    [JsonPropertyName("cNumOS")]
    public string? OsNumber { get; set; }

    [JsonPropertyName("cEtapa")]
    public string? Stage { get; set; }

    [JsonPropertyName("dDtPrevisao")]
    public string? ForecastDate { get; set; }

    [JsonPropertyName("nCodCli")]
    public long ClientCode { get; set; }

    // VERIFY (Omie test app): the exact NFS-e fields on ConsultarOS. When the OS is invoiced Omie fills the
    // NFS-e number/status; the caller may use this in addition to cEtapa to detect the billed state.
    [JsonPropertyName("cNumNFSe")]
    public string? InvoiceNumber { get; set; }

    [JsonPropertyName("cStatusNFSe")]
    public string? InvoiceStatus { get; set; }
}

public class ConsultOSProvidedService
{
    [JsonPropertyName("nCodServico")]
    public long ServiceCode { get; set; }

    [JsonPropertyName("nQtde")]
    public decimal Quantity { get; set; }

    [JsonPropertyName("nValUnit")]
    public decimal? UnitValue { get; set; }

    [JsonPropertyName("nSeqItem")]
    public int ItemSequence { get; set; }
}
