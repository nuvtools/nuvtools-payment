using System.Text.Json.Serialization;

namespace NuvTools.Payment.BancoDoBrasil.ApiClient.DTOs.Responses;

/// <summary>
/// Resposta de <c>GET /guias-codigo-barras/{idLancamento}</c> — situação de um lançamento de guia.
/// </summary>
public class GuiaPaymentResponse
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    /// <summary>
    /// Estado do lançamento em <b>texto</b> (ex.: "Consistente", "Pendente", "Agendado", "Pago",
    /// "Rejeitado", "Devolvido", "Cancelado", "Debitado", "Bloqueado"). NÃO é um código numérico.
    /// </summary>
    [JsonPropertyName("estadoPagamento")]
    public string? EstadoPagamento { get; set; }

    [JsonPropertyName("dataPagamento")]
    public long? DataPagamento { get; set; }

    [JsonPropertyName("valorPagamento")]
    public decimal? ValorPagamento { get; set; }

    [JsonPropertyName("codigoAutenticacaoPagamento")]
    public string? CodigoAutenticacaoPagamento { get; set; }

    [JsonPropertyName("listaDevolucao")]
    public List<GuiaPaymentDevolucao>? ListaDevolucao { get; set; }
}
