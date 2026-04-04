namespace NuvTools.Payment.Omie.ApiClient.Configuration;

/// <summary>
/// Configuracoes do cliente da API Omie.
/// </summary>
public class OmieApiClientConfig
{
    /// <summary>
    /// Nome da secao de configuracao.
    /// </summary>
    public const string SectionName = "Omie";

    public required string AppKey { get; set; }
    public required string AppSecret { get; set; }
    public required string BaseUrlClient { get; set; }
    public required string BaseUrlService { get; set; }
    public required string BaseUrlOrderService { get; set; }
    public required string BaseUrlOrderBilling { get; set; }
}
