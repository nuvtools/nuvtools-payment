using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NuvTools.Common.ResultWrapper;
using NuvTools.Payment.Contracts;
using NuvTools.Payment.Models.BankSlip;
using NuvTools.Payment.Omie.ApiClient.Configuration;
using NuvTools.Payment.Omie.ApiClient.Contracts;
using NuvTools.Payment.Omie.ApiClient.DTOs.Requests;
using NuvTools.Payment.Omie.ApiClient.DTOs.Responses;
using NuvTools.Payment.Omie.ApiClient.Mapping;
using NuvTools.Payment.Omie.ApiClient.Resources;

namespace NuvTools.Payment.Omie.ApiClient.Services;

/// <summary>
/// Default implementation of the Omie API client. Satisfies both the ERP-shaped
/// <see cref="IOmieApiClient"/> contract and the neutral <see cref="IBankSlipBilletQuery"/>.
/// </summary>
public class OmieApiClient(
    IOptions<OmieApiClientConfig> options,
    ILogger<OmieApiClient> logger) : IOmieApiClient
{
    private readonly OmieApiClientConfig _config = options.Value;

    // DO NOT register a typed HttpClient or AddStandardResilienceHandler for this client.
    // Omie's gateway returned generic SOAP "Bad Request" / "Consumo redundante" responses
    // when requests went through HttpClientFactory + Polly: HTTP/2 negotiation and Polly's
    // retry of duplicated successful requests both trip the gateway. The static client
    // below is the workaround — it forces HTTP/1.1 (see SendRawAsync) and bypasses the
    // resilience pipeline. The DI registration in DependencyInjection.cs reflects this:
    // the service is registered as a singleton with no typed HttpClient.
    private static readonly HttpClient _staticClient = new();

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

    public Task<IResult<GenerateBilletResponse>> GenerateBilletAsync(long? nCodTitulo = null, string? cCodIntTitulo = null, CancellationToken cancellationToken = default)
        => ExecuteBilletOperationAsync<GenerateBilletResponse>(Fields.GenerateBillet, nCodTitulo, cCodIntTitulo, Messages.WhenGeneratingOmieBillet, cancellationToken);

    public Task<IResult<GetBilletResponse>> GetBilletAsync(long? nCodTitulo = null, string? cCodIntTitulo = null, CancellationToken cancellationToken = default)
        => ExecuteBilletOperationAsync<GetBilletResponse>(Fields.GetBillet, nCodTitulo, cCodIntTitulo, Messages.WhenGettingOmieBillet, cancellationToken);

    #region IBankSlipBilletQuery (neutral surface)

    async Task<IResult<BankSlipBilletInfo>> IBankSlipBilletQuery.GetBilletAsync(BilletReference reference, CancellationToken cancellationToken)
    {
        var result = await GetBilletAsync(reference.OmieNumericId, reference.OmieIntegrationCode, cancellationToken);
        return result.Succeeded && result.Data is not null
            ? Result<BankSlipBilletInfo>.Success(result.Data.ToNeutral())
            : Result<BankSlipBilletInfo>.Fail(result.Message ?? "GetBillet failed.");
    }

    #endregion

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
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        // Force HTTP/1.1 — Omie's gateway returns generic SOAP "Bad Request" when
        // the request arrives over HTTP/2 (curl works because it negotiated 1.1 too).
        httpRequest.Version = HttpVersion.Version11;
        httpRequest.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

        // Content-Type exactly "application/json" (no ;charset=utf-8). StringContent's
        // overload would add the charset suffix which Omie's AWS ALB rejects.
        var bodyBytes = Encoding.UTF8.GetBytes(json);
        httpRequest.Content = new ByteArrayContent(bodyBytes);
        httpRequest.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

        // Curl always sends a User-Agent; some gateways behave differently without one.
        if (!httpRequest.Headers.Contains("User-Agent"))
            httpRequest.Headers.UserAgent.ParseAdd("nuvtools-payment-omie/1.0");

        try
        {
            return await _staticClient.SendAsync(httpRequest, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending request to Omie API: {Url}", url);
            return null;
        }
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
