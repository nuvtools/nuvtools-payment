using System.Text.Json.Serialization;

namespace NuvTools.Payment.Omie.ApiClient.DTOs.Responses;

/// <summary>
/// Response of ListarContratos (servicos/contrato) — the page of Service Contracts registered in Omie. A contract is
/// a price registry: it holds the negotiated value and the validity period agreed with one client, and must never be
/// billed through.
/// <para>
/// The listing exists so callers can resolve a contract by the number the user knows (<c>cNumCtr</c>) without asking
/// Omie one call per contract: Omie exposes no search by contract number, and repeated single consults are what trips
/// its "Consumo redundante" gate. List once, index in memory — the same approach as the service catalog.
/// </para>
/// </summary>
public class ListContractsResponse : IOmieBusinessStatus
{
    [JsonPropertyName("pagina")]
    public int Page { get; set; }

    [JsonPropertyName("total_de_paginas")]
    public int TotalPages { get; set; }

    [JsonPropertyName("registros")]
    public int Records { get; set; }

    [JsonPropertyName("total_de_registros")]
    public int TotalRecords { get; set; }

    [JsonPropertyName("contratoCadastro")]
    public ListContractsItem[]? Contracts { get; set; }

    [JsonPropertyName("cCodStatus")]
    public string? StatusCode { get; set; }

    [JsonPropertyName("cDescStatus")]
    public string? StatusDescription { get; set; }
}

/// <summary>One contract of the listing. Only the header is mapped — it carries everything a price registry needs.</summary>
public class ListContractsItem
{
    [JsonPropertyName("cabecalho")]
    public ListContractsHeader? Header { get; set; }
}

/// <summary>
/// The <c>cabecalho</c> of a contract in the listing: who it belongs to, the number the user recognises, the validity
/// period and the negotiated monthly value.
/// </summary>
public class ListContractsHeader
{
    /// <summary>Omie's internal identifier of the contract — the one ConsultarContrato takes.</summary>
    [JsonPropertyName("nCodCtr")]
    public long ContractCode { get; set; }

    /// <summary>Integration code, when the contract was created through the API.</summary>
    [JsonPropertyName("cCodIntCtr")]
    public string? IntegrationCode { get; set; }

    /// <summary>The contract number as the user reads it in Omie — how a caller's own configuration refers to it.</summary>
    [JsonPropertyName("cNumCtr")]
    public string? ContractNumber { get; set; }

    /// <summary>Client the contract belongs to (nCodCli).</summary>
    [JsonPropertyName("nCodCli")]
    public long ClientCode { get; set; }

    /// <summary>Start of the validity period, as Omie writes dates: dd/MM/yyyy.</summary>
    [JsonPropertyName("dVigInicial")]
    public string? ValidFrom { get; set; }

    /// <summary>End of the validity period (dd/MM/yyyy). Empty on contracts with no end date.</summary>
    [JsonPropertyName("dVigFinal")]
    public string? ValidTo { get; set; }

    /// <summary>Negotiated monthly value of the contract.</summary>
    [JsonPropertyName("nValTotMes")]
    public decimal? MonthlyValue { get; set; }

    /// <summary>Situation code of the contract in Omie.</summary>
    [JsonPropertyName("cCodSit")]
    public string? SituationCode { get; set; }
}
