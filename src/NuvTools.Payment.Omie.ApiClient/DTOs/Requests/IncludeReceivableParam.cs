using System.Text.Json.Serialization;

namespace NuvTools.Payment.Omie.ApiClient.DTOs.Requests;

/// <summary>
/// Main parameter for including an Accounts Receivable entry in the Omie API
/// (call: IncluirContaReceber). Unlike the Service Order flow, this endpoint
/// requires the document value and category code explicitly.
/// </summary>
public class IncludeReceivableParam
{
    [JsonPropertyName("codigo_lancamento_integracao")]
    public required string IntegrationEntryCode { get; set; }

    [JsonPropertyName("codigo_cliente_fornecedor")]
    public required long ClientSupplierCode { get; set; }

    [JsonPropertyName("data_vencimento")]
    public required string DueDate { get; set; }

    [JsonPropertyName("valor_documento")]
    public required decimal DocumentValue { get; set; }

    [JsonPropertyName("codigo_categoria")]
    public required string CategoryCode { get; set; }

    [JsonPropertyName("data_previsao")]
    public string? ForecastDate { get; set; }

    [JsonPropertyName("id_conta_corrente")]
    public long? CheckingAccountId { get; set; }

    [JsonPropertyName("data_emissao")]
    public string? IssueDate { get; set; }

    [JsonPropertyName("numero_documento")]
    public string? DocumentNumber { get; set; }

    [JsonPropertyName("numero_parcela")]
    public string? InstallmentNumber { get; set; }

    [JsonPropertyName("observacao")]
    public string? Notes { get; set; }
}
