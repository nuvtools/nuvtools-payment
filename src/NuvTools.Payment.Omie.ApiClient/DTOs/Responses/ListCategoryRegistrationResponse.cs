using System.Text.Json.Serialization;

namespace NuvTools.Payment.Omie.ApiClient.DTOs.Responses;

/// <summary>
/// Response of ListarCategorias (geral/categorias) — paged list of registered categories. Used to resolve a
/// service's cCodCateg to its category name (<c>descricao</c>). This endpoint pages with the generic
/// pagina/total_de_paginas keys.
/// </summary>
public class ListCategoryRegistrationResponse : IOmieBusinessStatus
{
    [JsonPropertyName("pagina")]
    public int Page { get; set; }

    [JsonPropertyName("total_de_paginas")]
    public int TotalPages { get; set; }

    [JsonPropertyName("registros")]
    public int Records { get; set; }

    [JsonPropertyName("total_de_registros")]
    public int TotalRecords { get; set; }

    [JsonPropertyName("categoria_cadastro")]
    public CategoryItem[]? Categories { get; set; }

    [JsonPropertyName("cCodStatus")]
    public string? StatusCode { get; set; }

    [JsonPropertyName("cDescStatus")]
    public string? StatusDescription { get; set; }
}

public class CategoryItem
{
    [JsonPropertyName("codigo")]
    public string? Code { get; set; }

    [JsonPropertyName("descricao")]
    public string? Description { get; set; }
}
