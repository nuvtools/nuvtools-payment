using System.Text.Json.Serialization;

namespace NuvTools.Payment.Sicoob.ApiClient.DTOs.Requests;

/// <summary>
/// Requisicao para criacao de boleto no Sicoob.
/// </summary>
public class CreateBankSlipRequest
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

    [JsonPropertyName("dataEmissao")]
    public string? DataEmissao { get; set; }

    [JsonPropertyName("tipoJurosMora")]
    public int TipoJurosMora { get; set; }

    [JsonPropertyName("valorJurosMora")]
    public decimal ValorJurosMora { get; set; }

    [JsonPropertyName("tipoMulta")]
    public int TipoMulta { get; set; }

    [JsonPropertyName("valorMulta")]
    public decimal ValorMulta { get; set; }

    [JsonPropertyName("tipoDesconto")]
    public int TipoDesconto { get; set; }

    [JsonPropertyName("valorDesconto")]
    public decimal ValorDesconto { get; set; }

    [JsonPropertyName("dataDesconto")]
    public string? DataDesconto { get; set; }

    [JsonPropertyName("numeroParcela")]
    public int NumeroParcela { get; set; }

    [JsonPropertyName("aceite")]
    public bool Aceite { get; set; }

    [JsonPropertyName("codigoNegativacao")]
    public int CodigoNegativacao { get; set; }

    [JsonPropertyName("numeroDiasNegativacao")]
    public int NumeroDiasNegativacao { get; set; }

    [JsonPropertyName("codigoProtesto")]
    public int CodigoProtesto { get; set; }

    [JsonPropertyName("numeroDiasProtesto")]
    public int NumeroDiasProtesto { get; set; }

    [JsonPropertyName("pagador")]
    public CreateBankSlipPayerRequest? Pagador { get; set; }
}

/// <summary>
/// Dados do pagador para criacao de boleto.
/// </summary>
public class CreateBankSlipPayerRequest
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

    [JsonPropertyName("email")]
    public string? Email { get; set; }
}
