using System.Text.Json.Serialization;

namespace NuvTools.Payment.Sicoob.ApiClient.DTOs.Responses;

/// <summary>
/// Resposta da consulta de boleto no Sicoob.
/// </summary>
public class BankSlipResponse
{
    [JsonPropertyName("numeroCliente")]
    public int NumeroCliente { get; set; }

    [JsonPropertyName("codigoModalidade")]
    public int CodigoModalidade { get; set; }

    [JsonPropertyName("nossoNumero")]
    public long NossoNumero { get; set; }

    [JsonPropertyName("seuNumero")]
    public string? SeuNumero { get; set; }

    [JsonPropertyName("dataVencimento")]
    public string? DataVencimento { get; set; }

    [JsonPropertyName("valor")]
    public decimal Valor { get; set; }

    [JsonPropertyName("valorAbatimento")]
    public decimal ValorAbatimento { get; set; }

    [JsonPropertyName("situacaoBoleto")]
    public int SituacaoBoleto { get; set; }

    [JsonPropertyName("linhaDigitavel")]
    public string? LinhaDigitavel { get; set; }

    [JsonPropertyName("codigoBarras")]
    public string? CodigoBarras { get; set; }

    [JsonPropertyName("pagador")]
    public BankSlipPayerResponse? Pagador { get; set; }
}
