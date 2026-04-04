using System.Text.Json.Serialization;

namespace NuvTools.Payment.BancoDoBrasil.ApiClient.DTOs.Requests;

/// <summary>
/// Requisicao para criacao de lote de pagamentos de boleto no BB.
/// </summary>
public class CreateBatchPaymentRequest
{
    [JsonPropertyName("numeroRequisicao")]
    public long NumeroRequisicao { get; set; }

    [JsonPropertyName("numeroAgenciaDebito")]
    public int NumeroAgenciaDebito { get; set; }

    [JsonPropertyName("numeroContaCorrenteDebito")]
    public long NumeroContaCorrenteDebito { get; set; }

    [JsonPropertyName("digitoVerificadorContaCorrenteDebito")]
    public string? DigitoVerificadorContaCorrenteDebito { get; set; }

    [JsonPropertyName("lancamentos")]
    public List<BatchPaymentItemRequest> Lancamentos { get; set; } = [];
}
