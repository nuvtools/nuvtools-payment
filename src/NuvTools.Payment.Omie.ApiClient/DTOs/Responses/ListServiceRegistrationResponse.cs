using System.Text.Json.Serialization;

namespace NuvTools.Payment.Omie.ApiClient.DTOs.Responses;

/// <summary>
/// Response of ListarCadastroServico (servicos/servico) — paged list of registered services. Used to resolve a
/// service's unit price/description by its nCodServ. The servicos/servico endpoint pages with the
/// n-prefixed keys (nPagina/nTotPaginas/nRegistros/nTotRegistros), not the generic pagina/total_de_paginas.
/// </summary>
public class ListServiceRegistrationResponse : IOmieBusinessStatus
{
    [JsonPropertyName("nPagina")]
    public int Page { get; set; }

    [JsonPropertyName("nTotPaginas")]
    public int TotalPages { get; set; }

    [JsonPropertyName("nRegistros")]
    public int Records { get; set; }

    [JsonPropertyName("nTotRegistros")]
    public int TotalRecords { get; set; }

    [JsonPropertyName("cadastros")]
    public ListServiceItem[]? Services { get; set; }

    [JsonPropertyName("cCodStatus")]
    public string? StatusCode { get; set; }

    [JsonPropertyName("cDescStatus")]
    public string? StatusDescription { get; set; }
}

public class ListServiceItem
{
    [JsonPropertyName("intListar")]
    public ListServiceIntList? IntList { get; set; }

    [JsonPropertyName("cabecalho")]
    public ListServiceItemHeader? Header { get; set; }
}

public class ListServiceIntList
{
    [JsonPropertyName("nCodServ")]
    public long ServiceCode { get; set; }
}

public class ListServiceItemHeader
{
    [JsonPropertyName("cCodigo")]
    public string? Code { get; set; }

    [JsonPropertyName("cCodCateg")]
    public string? CategoryCode { get; set; }

    [JsonPropertyName("cDescricao")]
    public string? Description { get; set; }

    [JsonPropertyName("nPrecoUnit")]
    public decimal UnitPrice { get; set; }
}
