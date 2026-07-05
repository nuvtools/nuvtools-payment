using System.Text.Json.Serialization;

namespace NuvTools.Payment.Omie.ApiClient.DTOs.Responses;

/// <summary>
/// Response of ConsultarContrato — the negotiated Service Contract of a client. The caller reads the
/// negotiated unit price per service (item) to price the OS for clients that have a contract (nCodCtr).
/// The contract is a price registry only and must never be billed through.
/// </summary>
public class ConsultContractResponse : IOmieBusinessStatus
{
    [JsonPropertyName("cabecalho")]
    public ConsultContractHeader? Header { get; set; }

    [JsonPropertyName("itensContrato")]
    public ConsultContractItem[]? Items { get; set; }

    [JsonPropertyName("cCodStatus")]
    public string? StatusCode { get; set; }

    [JsonPropertyName("cDescStatus")]
    public string? StatusDescription { get; set; }
}

public class ConsultContractHeader
{
    [JsonPropertyName("nCodCtr")]
    public long ContractCode { get; set; }

    [JsonPropertyName("cNumCtr")]
    public string? ContractNumber { get; set; }
}

/// <summary>
/// A single service line of the contract. VERIFY (Omie test app): ConsultarContrato nests item fields under
/// an "itemCabecalho" object — confirm the exact key names for the service code and negotiated unit price.
/// </summary>
public class ConsultContractItem
{
    [JsonPropertyName("itemCabecalho")]
    public ConsultContractItemHeader? Header { get; set; }
}

public class ConsultContractItemHeader
{
    [JsonPropertyName("codServico")]
    public long ServiceCode { get; set; }

    [JsonPropertyName("valorUnitario")]
    public decimal? UnitValue { get; set; }

    [JsonPropertyName("quantidade")]
    public decimal? Quantity { get; set; }
}
