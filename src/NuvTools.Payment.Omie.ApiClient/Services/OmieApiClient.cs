using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NuvTools.Common.ResultWrapper;
using NuvTools.Payment.Omie.ApiClient.Configuration;
using NuvTools.Payment.Omie.ApiClient.Contracts;
using NuvTools.Payment.Omie.ApiClient.DTOs.Requests;
using NuvTools.Payment.Omie.ApiClient.DTOs.Responses;
using NuvTools.Payment.Omie.ApiClient.Resources;

namespace NuvTools.Payment.Omie.ApiClient.Services;

/// <summary>
/// Default implementation of the Omie ERP API client.
/// </summary>
public class OmieApiClient(
    IOptions<OmieApiClientConfig> options,
    ILogger<OmieApiClient> logger) : IOmieApiClient
{
    private readonly OmieApiClientConfig _config = options.Value;

    // Canonical Omie endpoints for the calls added for the OS-upsert flow — same across all accounts.
    // Used only when the corresponding BaseUrl is not set in config.
    private const string DefaultContractUrl = "https://app.omie.com.br/api/v1/servicos/contrato/";
    private const string DefaultOrderStagesUrl = "https://app.omie.com.br/api/v1/servicos/osetapas/";
    private const string DefaultCategoryUrl = "https://app.omie.com.br/api/v1/geral/categorias/";

    // Throttle for batch runs: limits concurrent in-flight Omie requests (default 1 = sequential).
    private readonly SemaphoreSlim _throttle = new(Math.Max(1, options.Value.MaxConcurrentRequests));

    private string ContractUrl => string.IsNullOrWhiteSpace(_config.BaseUrlContract) ? DefaultContractUrl : _config.BaseUrlContract;
    private string OrderStagesUrl => string.IsNullOrWhiteSpace(_config.BaseUrlOrderStages) ? DefaultOrderStagesUrl : _config.BaseUrlOrderStages;
    private string CategoryUrl => string.IsNullOrWhiteSpace(_config.BaseUrlCategory) ? DefaultCategoryUrl : _config.BaseUrlCategory;

    /// <summary>Test-only constructor: routes sends through a mock <see cref="HttpMessageHandler"/>.</summary>
    internal OmieApiClient(IOptions<OmieApiClientConfig> options, ILogger<OmieApiClient> logger, HttpMessageHandler handler)
        : this(options, logger)
    {
        _httpClient = new HttpClient(handler);
    }

    // DO NOT register a typed HttpClient or AddStandardResilienceHandler for this client.
    // Omie's gateway returned generic SOAP "Bad Request" / "Consumo redundante" responses
    // when requests went through HttpClientFactory + Polly: HTTP/2 negotiation and Polly's
    // retry of duplicated successful requests both trip the gateway. The static client
    // below is the workaround — it forces HTTP/1.1 (see SendRawAsync) and bypasses the
    // resilience pipeline. The DI registration in DependencyInjection.cs reflects this:
    // the service is registered as a singleton with no typed HttpClient.
    private static readonly HttpClient _sharedClient = new();

    // Instance send client — defaults to the shared static client in production. Tests inject a mock
    // HttpMessageHandler through the internal constructor below (the only seam; there is no typed HttpClient).
    private HttpClient _httpClient = _sharedClient;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        // No PropertyNamingPolicy — Omie field names are explicit via [JsonPropertyName]
        // attributes on DTOs and verbatim on JsonObject envelope keys (call, app_key,
        // app_secret, param). A naming policy here would mangle envelope keys.
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<IResult<bool>> ConsultClientAsync(long omieClientCode, CancellationToken cancellationToken = default)
    {
        var request = BuildRequest(Fields.ConsultClient, new JsonArray(new JsonObject { ["codigo_cliente_omie"] = omieClientCode }));

        var response = await SendAsync(request, _config.BaseUrlClient, cancellationToken);

        if (response == null)
            return Result<bool>.Fail(string.Format(Messages.FailedCommunicationX, Messages.WhenConsultingOmieClient), logger: logger);

        if (response.IsSuccessStatusCode)
            return Result<bool>.Success(true);

        var errorMessage = await ParseErrorAsync(response, cancellationToken);
        return Result<bool>.Fail($"Omie ConsultarCliente failed for code {omieClientCode}: {errorMessage}", logger: logger);
    }

    public async Task<IResult<ConsultServiceRegistrationResponse>> ConsultServiceRegistrationAsync(long omieServiceCode, CancellationToken cancellationToken = default)
    {
        // Omie expects the envelope in the order: call, param, app_key, app_secret.
        // JsonObject preserves insertion order on serialize — DO NOT switch to anonymous
        // objects (their key order has tripped Omie before) or string interpolation
        // (JSON-injection risk on credentials with quotes/control chars).
        var paramItem = new JsonObject
        {
            ["cCodIntServ"] = null,
            ["nCodServ"] = omieServiceCode
        };
        var envelope = new JsonObject
        {
            ["call"] = Fields.ConsultServiceRegistration,
            ["param"] = new JsonArray(paramItem),
            ["app_key"] = _config.AppKey,
            ["app_secret"] = _config.AppSecret
        };
        var json = envelope.ToJsonString(JsonOptions);

        var response = await SendRawAsync(json, _config.BaseUrlService, cancellationToken);

        if (response == null)
            return Result<ConsultServiceRegistrationResponse>.Fail(string.Format(Messages.FailedCommunicationX, Messages.WhenConsultingOmieService), logger: logger);

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = ParseError(responseBody);
            return Result<ConsultServiceRegistrationResponse>.Fail(
                $"[HTTP {(int)response.StatusCode}] Omie ConsultarCadastroServico failed for service {omieServiceCode}: {errorMessage}",
                logger: logger);
        }

        var result = JsonSerializer.Deserialize<ConsultServiceRegistrationResponse>(responseBody, JsonOptions);

        return result == null
            ? Result<ConsultServiceRegistrationResponse>.Fail(string.Format(Messages.InvalidResponseFromOmieX, Fields.ConsultServiceRegistration), logger: logger)
            : Result<ConsultServiceRegistrationResponse>.Success(result);
    }

    public async Task<IResult<IncludeOSResponse>> IncludeOSAsync(IncludeOSParam param, CancellationToken cancellationToken = default)
    {
        return await ExecuteOmieOperationAsync<IncludeOSResponse>(
            Fields.IncludeOS,
            new JsonArray(JsonSerializer.SerializeToNode(param, JsonOptions)),
            _config.BaseUrlOrderService,
            Messages.WhenIncludingOmieWorkOrder,
            cancellationToken);
    }

    public async Task<IResult<IncludeReceivableResponse>> IncludeReceivableAsync(IncludeReceivableParam param, CancellationToken cancellationToken = default)
    {
        return await ExecuteOmieOperationAsync<IncludeReceivableResponse>(
            Fields.IncludeReceivable,
            new JsonArray(JsonSerializer.SerializeToNode(param, JsonOptions)),
            _config.BaseUrlReceivable,
            Messages.WhenIncludingOmieReceivable,
            cancellationToken);
    }

    public Task<IResult<ConsultContractResponse>> ConsultContractAsync(long omieContractCode, CancellationToken cancellationToken = default)
        => ExecuteOmieOperationAsync<ConsultContractResponse>(
            Fields.ConsultContract,
            new JsonArray(new JsonObject { ["nCodCtr"] = omieContractCode }),
            ContractUrl,
            Messages.WhenConsultingOmieContract,
            cancellationToken);

    public Task<IResult<ListServiceRegistrationResponse>> ListServiceRegistrationAsync(int page = 1, int recordsPerPage = 50, CancellationToken cancellationToken = default)
        => ExecuteOmieOperationAsync<ListServiceRegistrationResponse>(
            Fields.ListServiceRegistration,
            // servicos/servico ListarCadastroServico pages with nPagina/nRegPorPagina.
            new JsonArray(new JsonObject { ["nPagina"] = page, ["nRegPorPagina"] = recordsPerPage }),
            _config.BaseUrlService,
            Messages.WhenListingOmieServices,
            cancellationToken);

    public Task<IResult<ListCategoryRegistrationResponse>> ListCategoryRegistrationAsync(int page = 1, int recordsPerPage = 1000, string description = "", CancellationToken cancellationToken = default)
        => ExecuteOmieOperationAsync<ListCategoryRegistrationResponse>(
            Fields.ListCategoryRegistration,
            new JsonArray(new JsonObject { ["pagina"] = page, ["registros_por_pagina"] = recordsPerPage, ["descricao"] = description }),
            CategoryUrl,
            Messages.WhenListingOmieCategories,
            cancellationToken);

    public Task<IResult<IncludeOSResponse>> ChangeOSAsync(IncludeOSParam param, CancellationToken cancellationToken = default)
        => ExecuteOmieOperationAsync<IncludeOSResponse>(
            Fields.ChangeOS,
            new JsonArray(JsonSerializer.SerializeToNode(param, JsonOptions)),
            _config.BaseUrlOrderService,
            Messages.WhenChangingOmieWorkOrder,
            cancellationToken);

    public Task<IResult<ConsultOSResponse>> ConsultOSAsync(long? nCodOS = null, string? cCodIntOS = null, CancellationToken cancellationToken = default)
    {
        if (nCodOS is null && string.IsNullOrWhiteSpace(cCodIntOS))
            return Task.FromResult<IResult<ConsultOSResponse>>(
                Result<ConsultOSResponse>.Fail("Provide nCodOS or cCodIntOS.", logger: logger));

        // Both keys present, exactly one filled (same rule as the billet lookups).
        var param = new JsonObject();
        if (nCodOS.HasValue)
        {
            param["nCodOS"] = nCodOS.Value;
            param["cCodIntOS"] = null;
        }
        else
        {
            param["nCodOS"] = null;
            param["cCodIntOS"] = cCodIntOS;
        }

        return ExecuteOmieOperationAsync<ConsultOSResponse>(
            Fields.ConsultOS,
            new JsonArray(param),
            _config.BaseUrlOrderService,
            Messages.WhenConsultingOmieWorkOrder,
            cancellationToken);
    }

    public Task<IResult<ChangeOSStageResponse>> ChangeOSStageAsync(ChangeOSStageParam param, CancellationToken cancellationToken = default)
        => ExecuteOmieOperationAsync<ChangeOSStageResponse>(
            Fields.ChangeOSStage,
            new JsonArray(JsonSerializer.SerializeToNode(param, JsonOptions)),
            _config.BaseUrlOrderService,
            Messages.WhenChangingOmieWorkOrderStage,
            cancellationToken);

    public Task<IResult<ListBillingStagesResponse>> ListBillingStagesAsync(CancellationToken cancellationToken = default)
        => ExecuteOmieOperationAsync<ListBillingStagesResponse>(
            Fields.ListBillingStages,
            new JsonArray(new JsonObject { ["pagina"] = 1, ["registros_por_pagina"] = 100 }),
            OrderStagesUrl,
            Messages.WhenListingOmieBillingStages,
            cancellationToken);

    public Task<IResult<GenerateBilletResponse>> GenerateBilletAsync(long? nCodTitulo = null, string? cCodIntTitulo = null, CancellationToken cancellationToken = default)
        => ExecuteBilletOperationAsync<GenerateBilletResponse>(Fields.GenerateBillet, nCodTitulo, cCodIntTitulo, Messages.WhenGeneratingOmieBillet, cancellationToken);

    public Task<IResult<GetBilletResponse>> GetBilletAsync(long? nCodTitulo = null, string? cCodIntTitulo = null, CancellationToken cancellationToken = default)
        => ExecuteBilletOperationAsync<GetBilletResponse>(Fields.GetBillet, nCodTitulo, cCodIntTitulo, Messages.WhenGettingOmieBillet, cancellationToken);

    private async Task<IResult<TResponse>> ExecuteBilletOperationAsync<TResponse>(
        string operation,
        long? nCodTitulo,
        string? cCodIntTitulo,
        string failureContext,
        CancellationToken cancellationToken)
        where TResponse : IOmieBusinessStatus
    {
        if (nCodTitulo is null && string.IsNullOrWhiteSpace(cCodIntTitulo))
            return Result<TResponse>.Fail(Messages.ProvideTituloIdentifier, logger: logger);

        // Omie [103]: "Preencha apenas a tag [nCodTitulo] ou a tag [cCodIntTitulo]".
        // Both keys must be present, but EXACTLY ONE filled — the other as literal null.
        // Prefer nCodTitulo (numeric/authoritative) when both are provided by the caller.
        var param = new JsonObject();
        if (nCodTitulo.HasValue)
        {
            param["nCodTitulo"] = nCodTitulo.Value;
            param["cCodIntTitulo"] = null;
        }
        else
        {
            param["nCodTitulo"] = null;
            param["cCodIntTitulo"] = cCodIntTitulo;
        }

        return await ExecuteOmieOperationAsync<TResponse>(
            operation,
            new JsonArray(param),
            _config.BaseUrlBilletReceivable,
            failureContext,
            cancellationToken);
    }

    private async Task<IResult<TResponse>> ExecuteOmieOperationAsync<TResponse>(
        string operation,
        JsonArray param,
        string url,
        string failureContext,
        CancellationToken cancellationToken)
        where TResponse : IOmieBusinessStatus
    {
        var request = BuildRequest(operation, param);

        var response = await SendAsync(request, url, cancellationToken);

        if (response == null)
            return Result<TResponse>.Fail(string.Format(Messages.FailedCommunicationX, failureContext), logger: logger);

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = ParseError(responseBody);
            return Result<TResponse>.Fail($"[HTTP {(int)response.StatusCode}] Omie {operation}: {errorMessage}", logger: logger);
        }

        var result = JsonSerializer.Deserialize<TResponse>(responseBody, JsonOptions);

        if (result == null)
            return Result<TResponse>.Fail(string.Format(Messages.InvalidResponseFromOmieX, operation), logger: logger);

        var (isFailure, errorBody) = ValidateOmieBusinessStatus(result.StatusCode, result.StatusDescription, operation);
        return isFailure
            ? Result<TResponse>.Fail(errorBody, logger: logger)
            : Result<TResponse>.Success(result);
    }

    private JsonObject BuildRequest(string call, JsonArray param)
    {
        // Same envelope shape as ConsultServiceRegistrationAsync — JsonObject preserves
        // insertion order so the canonical (call, param, app_key, app_secret) sequence
        // is stable. Anonymous objects worked but their key order is fragile across runtimes.
        return new JsonObject
        {
            ["call"] = call,
            ["param"] = param,
            ["app_key"] = _config.AppKey,
            ["app_secret"] = _config.AppSecret
        };
    }

    private async Task<HttpResponseMessage?> SendAsync(JsonObject requestBody, string url, CancellationToken cancellationToken)
    {
        var json = requestBody.ToJsonString(JsonOptions);
        return await SendRawAsync(json, url, cancellationToken);
    }

    private async Task<HttpResponseMessage?> SendRawAsync(string json, string url, CancellationToken cancellationToken)
    {
        // Content-Type exactly "application/json" (no ;charset=utf-8). StringContent's overload would add the
        // charset suffix which Omie's AWS ALB rejects. Reused across retry attempts (fresh request per attempt).
        var bodyBytes = Encoding.UTF8.GetBytes(json);
        var maxAttempts = Math.Max(1, _config.MaxRetryAttempts + 1);

        // Throttle concurrent Omie requests (batch runs). Default 1 => strictly sequential.
        await _throttle.WaitAsync(cancellationToken);
        try
        {
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
                // Force HTTP/1.1 — Omie's gateway returns generic SOAP "Bad Request" over HTTP/2.
                httpRequest.Version = HttpVersion.Version11;
                httpRequest.VersionPolicy = HttpVersionPolicy.RequestVersionExact;
                httpRequest.Content = new ByteArrayContent(bodyBytes);
                httpRequest.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                if (!httpRequest.Headers.Contains("User-Agent"))
                    httpRequest.Headers.UserAgent.ParseAdd("nuvtools-payment-omie/1.0");

                try
                {
                    var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

                    // Retry only transient server-side failures (5xx). 2xx and 4xx (incl. business faults) are
                    // returned as-is. Retrying OS writes is safe: cCodIntOS makes IncluirOS idempotent and
                    // AlterarOS replaces items wholesale.
                    if ((int)response.StatusCode >= 500 && attempt < maxAttempts)
                    {
                        response.Dispose();
                        logger.LogWarning("Omie API {Url} returned HTTP {Status} (attempt {Attempt}/{Max}); retrying.",
                            url, (int)response.StatusCode, attempt, maxAttempts);
                        await DelayBeforeRetryAsync(attempt, cancellationToken);
                        continue;
                    }

                    return response;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    if (attempt < maxAttempts)
                    {
                        logger.LogWarning(ex, "Transient error sending to Omie API {Url} (attempt {Attempt}/{Max}); retrying.",
                            url, attempt, maxAttempts);
                        await DelayBeforeRetryAsync(attempt, cancellationToken);
                        continue;
                    }

                    logger.LogError(ex, "Error sending request to Omie API: {Url}", url);
                    return null;
                }
            }

            return null;
        }
        finally
        {
            _throttle.Release();
        }
    }

    private async Task DelayBeforeRetryAsync(int attempt, CancellationToken cancellationToken)
    {
        var delay = Math.Max(0, _config.RetryDelayMilliseconds) * attempt;
        if (delay > 0)
            await Task.Delay(delay, cancellationToken);
    }

    /// <summary>
    /// Validates the business-level status returned by Omie in the response body
    /// (cCodStatus / codigo_status). Omie typically returns HTTP 200 even when the
    /// operation didn't succeed at the business level — the status code in the body
    /// is what tells the real outcome. Status "0" (or empty) means success; anything
    /// else is treated as a failure with the description from cDesStatus / descricao_status.
    /// </summary>
    private static (bool isFailure, string errorMessage) ValidateOmieBusinessStatus(
        string? statusCode,
        string? statusDescription,
        string operation)
    {
        if (string.IsNullOrWhiteSpace(statusCode) || statusCode == "0")
            return (false, string.Empty);

        var detail = !string.IsNullOrWhiteSpace(statusDescription)
            ? statusDescription
            : string.Format(Messages.OmieReturnedStatusXWithoutDescription, statusCode);

        return (true, $"[{statusCode}] {operation}: {detail}");
    }

    private static string ParseError(string responseBody)
    {
        try
        {
            var error = JsonSerializer.Deserialize<OmieErrorResponse>(responseBody, JsonOptions);
            if (!string.IsNullOrEmpty(error?.FaultString))
                return error.FaultString;
        }
        catch
        {
            // Ignore deserialization error
        }

        return !string.IsNullOrEmpty(responseBody)
            ? responseBody
            : Messages.UnknownErrorFromOmie;
    }

    private static async Task<string> ParseErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        return ParseError(responseBody);
    }
}
