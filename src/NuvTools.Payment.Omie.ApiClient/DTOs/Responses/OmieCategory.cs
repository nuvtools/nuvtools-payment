namespace NuvTools.Payment.Omie.ApiClient.DTOs.Responses;

/// <summary>A usable account of the Omie chart of accounts (<c>ListarCategorias</c>).</summary>
/// <param name="Code">Category code (<c>codigo</c>) — the <c>cCodCateg</c> a service order carries.</param>
/// <param name="Description">Category description as registered in the chart of accounts.</param>
public record OmieCategory(string Code, string Description);
