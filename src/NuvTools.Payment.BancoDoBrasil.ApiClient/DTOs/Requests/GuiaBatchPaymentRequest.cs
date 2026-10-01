using System.Text.Json.Serialization;

namespace NuvTools.Payment.BancoDoBrasil.ApiClient.DTOs.Requests;

/// <summary>
/// Requisição de <c>POST /lotes-guias-codigo-barras</c> — solicita o pagamento em lote de guias de
/// arrecadação com código de barras (ex.: taxa/guia do Detran).
/// </summary>
public class GuiaBatchPaymentRequest
{
    /// <summary>Identificação da solicitação: número único, não sequencial, obtido em
    /// <c>GET /proximos-numeros-requisicao</c>.</summary>
    [JsonPropertyName("numeroRequisicao")]
    public long NumeroRequisicao { get; set; }

    /// <summary>Contrato de pagamento entre o conveniado e o Banco do Brasil.</summary>
    [JsonPropertyName("codigoContrato")]
    public long CodigoContrato { get; set; }

    [JsonPropertyName("numeroAgenciaDebito")]
    public int NumeroAgenciaDebito { get; set; }

    [JsonPropertyName("numeroContaCorrenteDebito")]
    public long NumeroContaCorrenteDebito { get; set; }

    [JsonPropertyName("digitoVerificadorContaCorrenteDebito")]
    public string? DigitoVerificadorContaCorrenteDebito { get; set; }

    [JsonPropertyName("lancamentos")]
    public List<GuiaBatchPaymentItem> Lancamentos { get; set; } = [];
}
