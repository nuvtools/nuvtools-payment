namespace NuvTools.Payment.Omie.ApiClient.Configuration;

/// <summary>
/// Configuration for the Omie API client.
/// </summary>
public class OmieApiClientConfig
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Omie";

    public required string AppKey { get; set; }
    public required string AppSecret { get; set; }

    /// <summary>
    /// Optional Marketplace app hash. When the Omie app is registered as a
    /// marketplace integration, Omie requires this hash on every request envelope
    /// alongside app_key and app_secret. Leave null/empty for non-marketplace apps.
    /// </summary>
    public string? AppHash { get; set; }

    public required string BaseUrlClient { get; set; }
    public required string BaseUrlService { get; set; }
    public required string BaseUrlOrderService { get; set; }
    public required string BaseUrlOrderBilling { get; set; }
    public required string BaseUrlReceivable { get; set; }
    public required string BaseUrlBilletReceivable { get; set; }
}
