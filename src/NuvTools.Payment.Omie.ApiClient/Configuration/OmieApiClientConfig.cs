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

    /// <summary>
    /// Base URL shared by every Omie endpoint (e.g. <c>https://app.omie.com.br/api/v1/</c>).
    /// The <c>Endpoint*</c> paths below are relative to it and concatenated onto it at call time.
    /// </summary>
    public required string BaseUrl { get; set; }

    // Endpoint paths relative to BaseUrl. Defaults are Omie's canonical public paths — stable and the
    // same across all accounts (only app_key/app_secret differ). Override via config only if Omie changes them.
    /// <summary>geral/clientes/ — cliente registration.</summary>
    public string EndpointClient { get; set; } = "geral/clientes/";

    /// <summary>servicos/servico/ — service registration/catalog.</summary>
    public string EndpointService { get; set; } = "servicos/servico/";

    /// <summary>servicos/os/ — Service Order (OS) upsert/consult.</summary>
    public string EndpointOrderService { get; set; } = "servicos/os/";

    /// <summary>servicos/osetapas/ — OS billing stages.</summary>
    public string EndpointOrderBilling { get; set; } = "servicos/osetapas/";

    /// <summary>financas/contareceber/ — receivables.</summary>
    public string EndpointReceivable { get; set; } = "financas/contareceber/";

    /// <summary>financas/contareceberboleto/ — receivable billets.</summary>
    public string EndpointBilletReceivable { get; set; } = "financas/contareceberboleto/";

    /// <summary>servicos/contrato/ — ConsultarContrato (negotiated unit price per service item).</summary>
    public string EndpointContract { get; set; } = "servicos/contrato/";

    /// <summary>servicos/osetapas/ — ListarEtapasFaturamento (Kanban stage discovery).</summary>
    public string EndpointOrderStages { get; set; } = "servicos/osetapas/";

    /// <summary>geral/categorias/ — ListarCategorias (resolve a service's cCodCateg to its category name).</summary>
    public string EndpointCategory { get; set; } = "geral/categorias/";

    /// <summary>geral/contacorrente/ — ListarContasCorrentes (checking accounts a receivable can land on).</summary>
    public string EndpointCheckingAccount { get; set; } = "geral/contacorrente/";

    /// <summary>
    /// servicos/osp/ — ValidarOS and FaturarOS, which invoice a single service order. Not to be confused with
    /// servicos/oslote/, which invoices every OS of a Kanban stage at once, nor with servicos/osetapas/, which
    /// only lists stages.
    /// </summary>
    public string EndpointOrderServiceBilling { get; set; } = "servicos/osp/";

    /// <summary>Page size when listing clients — ListarClientes accepts up to 500. Default 500.</summary>
    public int ClientsPerPage { get; set; } = 500;

    /// <summary>Page size when listing categories. Default 500.</summary>
    public int CategoriesPerPage { get; set; } = 500;

    // Minimal resilience knobs (this client intentionally does NOT use Polly/HttpClientFactory — see the
    // comment on OmieApiClient._staticClient). Retries cover only network failures and HTTP 5xx; the
    // deterministic cCodIntOS idempotency anchor makes retrying OS writes safe. Set MaxRetryAttempts = 0 to disable.
    /// <summary>Max retry attempts on transient network/5xx failures. Default 2. 0 disables retries.</summary>
    public int MaxRetryAttempts { get; set; } = 2;

    /// <summary>Base delay (ms) between retries; grows linearly with the attempt number. Default 500.</summary>
    public int RetryDelayMilliseconds { get; set; } = 500;

    /// <summary>Max concurrent in-flight Omie requests (throttle for batch runs). Default 1 (sequential).</summary>
    public int MaxConcurrentRequests { get; set; } = 1;

    /// <summary>Combines <see cref="BaseUrl"/> with a relative endpoint path, normalizing the slash between them.</summary>
    public string ResolveUrl(string endpoint)
        => $"{BaseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";
}
