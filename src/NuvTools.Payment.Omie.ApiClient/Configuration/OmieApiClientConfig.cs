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

    // Endpoints added for the Service Order (OS) upsert flow. Optional so existing consumers keep binding;
    // when unset the client falls back to Omie's canonical public URLs (they are stable and the same across
    // all accounts — only app_key/app_secret differ). Override via config only if Omie changes them.
    /// <summary>servicos/contrato/ — ConsultarContrato (negotiated unit price per service item).</summary>
    public string? BaseUrlContract { get; set; }

    /// <summary>servicos/osetapas/ — ListarEtapasFaturamento (Kanban stage discovery).</summary>
    public string? BaseUrlOrderStages { get; set; }

    /// <summary>geral/categorias/ — ListarCategorias (resolve a service's cCodCateg to its category name).</summary>
    public string? BaseUrlCategory { get; set; }

    // Minimal resilience knobs (this client intentionally does NOT use Polly/HttpClientFactory — see the
    // comment on OmieApiClient._staticClient). Retries cover only network failures and HTTP 5xx; the
    // deterministic cCodIntOS idempotency anchor makes retrying OS writes safe. Set MaxRetryAttempts = 0 to disable.
    /// <summary>Max retry attempts on transient network/5xx failures. Default 2. 0 disables retries.</summary>
    public int MaxRetryAttempts { get; set; } = 2;

    /// <summary>Base delay (ms) between retries; grows linearly with the attempt number. Default 500.</summary>
    public int RetryDelayMilliseconds { get; set; } = 500;

    /// <summary>Max concurrent in-flight Omie requests (throttle for batch runs). Default 1 (sequential).</summary>
    public int MaxConcurrentRequests { get; set; } = 1;
}
