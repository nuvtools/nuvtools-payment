using System.Text.Json.Serialization;

namespace NuvTools.Payment.BancoDoBrasil.ApiClient.DTOs.Responses;

/// <summary>
/// Lançamento retornado na criação/consulta de um lote de guias com código de barras.
/// </summary>
public class GuiaBatchPaymentResponseItem
{
    /// <summary>Identificação do lançamento atribuída pelo Banco — usada na consulta
    /// <c>GET /guias-codigo-barras/{idLancamento}</c>.</summary>
    [JsonPropertyName("codigoIdentificadorPagamento")]
    public long CodigoIdentificadorPagamento { get; set; }

    [JsonPropertyName("nomeBeneficiario")]
    public string? NomeBeneficiario { get; set; }

    [JsonPropertyName("codigoBarras")]
    public string? CodigoBarras { get; set; }

    /// <summary>Indicador de aceite dos dados: "S" = aceito; "N" = recusado (ver <see cref="Errors"/>).</summary>
    [JsonPropertyName("indicadorAceite")]
    public string? IndicadorAceite { get; set; }

    /// <summary>Códigos de erro por posição (0 = sem erro).</summary>
    [JsonPropertyName("errors")]
    public List<int>? Errors { get; set; }
}
