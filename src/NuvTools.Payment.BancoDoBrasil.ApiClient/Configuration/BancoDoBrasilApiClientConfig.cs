namespace NuvTools.Payment.BancoDoBrasil.ApiClient.Configuration;

/// <summary>
/// Configuracoes do cliente da API Banco do Brasil.
/// </summary>
public class BancoDoBrasilApiClientConfig
{
    /// <summary>
    /// Nome da secao de configuracao.
    /// </summary>
    public const string SectionName = "BancoDoBrasil";

    public required string AuthUrl { get; set; }
    public required string BaseUrl { get; set; }
    public required string ClientId { get; set; }
    public required string ClientSecret { get; set; }
    public required string ApiKey { get; set; }
}
