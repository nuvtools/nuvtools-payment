using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NuvTools.Payment.Omie.ApiClient.Configuration;
using NuvTools.Payment.Omie.ApiClient.Contracts;
using NuvTools.Payment.Omie.ApiClient.DTOs.Requests;
using NuvTools.Payment.Omie.ApiClient.DTOs.Responses;
using NuvTools.Payment.Omie.ApiClient.Resources;

namespace NuvTools.Payment.Omie.ApiClient.Services;

/// <summary>
/// Default implementation of the Omie API client.
/// </summary>
public class OmieApiClient(
    HttpClient httpClient,
    IOptions<OmieApiClientConfig> options,
    ILogger<OmieApiClient> logger) : IOmieApiClient
{
    private readonly OmieApiClientConfig _config = options.Value;

    // Static HttpClient (no factory, no Polly resilience) — the factory-injected
    // httpClient was triggering generic SOAP "Bad Request" responses from Omie's
    // gateway. Bypassing it solves the issue for all calls.
    // The factory-injected `httpClient` parameter is retained for backwards compat
    // but is not used.
    private static readonly HttpClient _staticClient = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        // No PropertyNamingPolicy — Omie field names are explicit via [JsonPropertyName]
        // attributes on DTOs and verbatim on anonymous request envelopes (call, app_key,
        // app_secret, param). A naming policy here can mangle envelope keys.
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<OmieApiResult<bool>> ConsultClientAsync(long omieClientCode, CancellationToken cancellationToken = default)
    {
        var request = BuildRequest(Fields.ConsultClient, new[] { new { codigo_cliente_omie = omieClientCode } });

        var response = await SendAsync(request, _config.BaseUrlClient, cancellationToken);

        if (response == null)
            return OmieApiResult<bool>.Fail(string.Format(Messages.FailedCommunicationX, Messages.WhenConsultingOmieClient));

        if (response.IsSuccessStatusCode)
            return OmieApiResult<bool>.Success(true);

        var errorMessage = await ParseErrorAsync(response, cancellationToken);
        logger.LogWarning("Omie ConsultClient failed for code {OmieClientCode}: {Error}", omieClientCode, errorMessage);
        return OmieApiResult<bool>.Fail(errorMessage);
    }

    public async Task<OmieApiResult<ConsultServiceRegistrationResponse>> ConsultServiceRegistrationAsync(long omieServiceCode, CancellationToken cancellationToken = default)
    {
        // Body built literally to match the Omie template byte-for-byte (field order:
        // call, param, app_key, app_secret). cCodIntServ must be present as null.
        var literalJson = $$"""
            {"call":"{{Fields.ConsultServiceRegistration}}","param":[{"cCodIntServ":null,"nCodServ":{{omieServiceCode}}}],"app_key":"{{_config.AppKey}}","app_secret":"{{_config.AppSecret}}"}
            """;

        var response = await SendRawAsync(literalJson, _config.BaseUrlService, cancellationToken);

        if (response == null)
            return OmieApiResult<ConsultServiceRegistrationResponse>.Fail(string.Format(Messages.FailedCommunicationX, Messages.WhenConsultingOmieService));

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = ParseError(responseBody);
            return OmieApiResult<ConsultServiceRegistrationResponse>.Fail(
                $"[HTTP {(int)response.StatusCode}] {errorMessage}");
        }

        var result = JsonSerializer.Deserialize<ConsultServiceRegistrationResponse>(responseBody, JsonOptions);

        if (result == null)
            return OmieApiResult<ConsultServiceRegistrationResponse>.Fail(string.Format(Messages.InvalidResponseFromOmieX, Fields.ConsultServiceRegistration));

        return OmieApiResult<ConsultServiceRegistrationResponse>.Success(result);
    }

    public async Task<OmieApiResult<IncludeOSResponse>> IncludeOSAsync(IncludeOSParam param, CancellationToken cancellationToken = default)
    {
        var request = BuildRequest(Fields.IncludeOS, new[] { param });

        var response = await SendAsync(request, _config.BaseUrlOrderService, cancellationToken);

        if (response == null)
            return OmieApiResult<IncludeOSResponse>.Fail(string.Format(Messages.FailedCommunicationX, Messages.WhenIncludingOmieWorkOrder));

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = ParseError(responseBody);
            logger.LogWarning("Omie IncluirOS failed: {Error}", errorMessage);
            return OmieApiResult<IncludeOSResponse>.Fail(errorMessage);
        }

        var result = JsonSerializer.Deserialize<IncludeOSResponse>(responseBody, JsonOptions);

        if (result == null)
            return OmieApiResult<IncludeOSResponse>.Fail(string.Format(Messages.InvalidResponseFromOmieX, Fields.IncludeOS));

        var (osIsFailure, osErrorMessage) = ValidateOmieBusinessStatus(result.StatusCode, result.StatusDescription, Fields.IncludeOS);
        if (osIsFailure)
            return OmieApiResult<IncludeOSResponse>.Fail(osErrorMessage);

        return OmieApiResult<IncludeOSResponse>.Success(result);
    }

    public async Task<OmieApiResult<IncludeReceivableResponse>> IncludeReceivableAsync(IncludeReceivableParam param, CancellationToken cancellationToken = default)
    {
        var request = BuildRequest(Fields.IncludeReceivable, new[] { param });

        var response = await SendAsync(request, _config.BaseUrlReceivable, cancellationToken);

        if (response == null)
            return OmieApiResult<IncludeReceivableResponse>.Fail(string.Format(Messages.FailedCommunicationX, Messages.WhenIncludingOmieReceivable));

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = ParseError(responseBody);
            logger.LogWarning("Omie IncluirContaReceber failed for integration key {IntegrationKey}: {Error}", param.IntegrationEntryCode, errorMessage);
            return OmieApiResult<IncludeReceivableResponse>.Fail(errorMessage);
        }

        var result = JsonSerializer.Deserialize<IncludeReceivableResponse>(responseBody, JsonOptions);

        if (result == null)
            return OmieApiResult<IncludeReceivableResponse>.Fail(string.Format(Messages.InvalidResponseFromOmieX, Fields.IncludeReceivable));

        var (receivableIsFailure, receivableErrorMessage) = ValidateOmieBusinessStatus(result.StatusCode, result.StatusDescription, Fields.IncludeReceivable);
        if (receivableIsFailure)
            return OmieApiResult<IncludeReceivableResponse>.Fail(receivableErrorMessage);

        return OmieApiResult<IncludeReceivableResponse>.Success(result);
    }

    public async Task<OmieApiResult<GenerateBilletResponse>> GenerateBilletAsync(long? nCodTitulo = null, string? cCodIntTitulo = null, CancellationToken cancellationToken = default)
    {
        if (nCodTitulo is null && string.IsNullOrWhiteSpace(cCodIntTitulo))
            return OmieApiResult<GenerateBilletResponse>.Fail(Messages.ProvideTituloIdentifier);

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
        var request = BuildRequest(Fields.GenerateBillet, new JsonArray(param));

        var response = await SendAsync(request, _config.BaseUrlBilletReceivable, cancellationToken);

        if (response == null)
            return OmieApiResult<GenerateBilletResponse>.Fail(string.Format(Messages.FailedCommunicationX, Messages.WhenGeneratingOmieBillet));

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = ParseError(responseBody);
            return OmieApiResult<GenerateBilletResponse>.Fail($"[HTTP {(int)response.StatusCode}] {errorMessage}");
        }

        var result = JsonSerializer.Deserialize<GenerateBilletResponse>(responseBody, JsonOptions);

        if (result == null)
            return OmieApiResult<GenerateBilletResponse>.Fail(string.Format(Messages.InvalidResponseFromOmieX, Fields.GenerateBillet));

        var (billetIsFailure, billetErrorMessage) = ValidateOmieBusinessStatus(result.StatusCode, result.StatusDescription, Fields.GenerateBillet);
        if (billetIsFailure)
            return OmieApiResult<GenerateBilletResponse>.Fail(billetErrorMessage);

        return OmieApiResult<GenerateBilletResponse>.Success(result);
    }

    public async Task<OmieApiResult<GetBilletResponse>> GetBilletAsync(long? nCodTitulo = null, string? cCodIntTitulo = null, CancellationToken cancellationToken = default)
    {
        if (nCodTitulo is null && string.IsNullOrWhiteSpace(cCodIntTitulo))
            return OmieApiResult<GetBilletResponse>.Fail(Messages.ProvideTituloIdentifier);

        // Same rule as GerarBoleto: exactly one identifier filled, the other literal null.
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
        var request = BuildRequest(Fields.GetBillet, new JsonArray(param));

        var response = await SendAsync(request, _config.BaseUrlBilletReceivable, cancellationToken);

        if (response == null)
            return OmieApiResult<GetBilletResponse>.Fail(string.Format(Messages.FailedCommunicationX, Messages.WhenGettingOmieBillet));

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = ParseError(responseBody);
            return OmieApiResult<GetBilletResponse>.Fail($"[HTTP {(int)response.StatusCode}] {errorMessage}");
        }

        var result = JsonSerializer.Deserialize<GetBilletResponse>(responseBody, JsonOptions);

        if (result == null)
            return OmieApiResult<GetBilletResponse>.Fail(string.Format(Messages.InvalidResponseFromOmieX, Fields.GetBillet));

        var (getBilletIsFailure, getBilletErrorMessage) = ValidateOmieBusinessStatus(result.StatusCode, result.StatusDescription, Fields.GetBillet);
        if (getBilletIsFailure)
            return OmieApiResult<GetBilletResponse>.Fail(getBilletErrorMessage);

        return OmieApiResult<GetBilletResponse>.Success(result);
    }

    private object BuildRequest<T>(string call, T param)
    {
        return new
        {
            call,
            app_key = _config.AppKey,
            app_secret = _config.AppSecret,
            param
        };
    }

    private async Task<HttpResponseMessage?> SendAsync(object requestBody, string url, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(requestBody, JsonOptions);
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
            // Use the static HttpClient (no factory, no Polly resilience). The
            // factory-injected client triggered Omie's "Consumo redundante" / SOAP
            // "Bad Request" responses because Polly retries duplicated successful
            // requests on transient blips.
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
