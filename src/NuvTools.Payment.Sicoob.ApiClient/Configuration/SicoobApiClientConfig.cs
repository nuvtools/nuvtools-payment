namespace NuvTools.Payment.Sicoob.ApiClient.Configuration;

/// <summary>
/// Configuracoes do cliente da API Sicoob.
/// </summary>
public class SicoobApiClientConfig
{
    /// <summary>
    /// Nome da secao de configuracao.
    /// </summary>
    public const string SectionName = "Sicoob";

    public required string BaseUrl { get; set; }
    public required string ClientId { get; set; }
    public required string Token { get; set; }
}
