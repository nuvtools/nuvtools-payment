using System.Text.Json.Serialization;

namespace NuvTools.Payment.Sicoob.ApiClient.DTOs.Responses;

/// <summary>
/// Dados do pagador na resposta de boleto do Sicoob.
/// </summary>
public class BankSlipPayerResponse
{
    [JsonPropertyName("numeroCpfCnpj")]
    public string? NumeroCpfCnpj { get; set; }

    [JsonPropertyName("nome")]
    public string? Nome { get; set; }

    [JsonPropertyName("endereco")]
    public string? Endereco { get; set; }

    [JsonPropertyName("bairro")]
    public string? Bairro { get; set; }

    [JsonPropertyName("cidade")]
    public string? Cidade { get; set; }

    [JsonPropertyName("cep")]
    public string? Cep { get; set; }

    [JsonPropertyName("uf")]
    public string? Uf { get; set; }
}
