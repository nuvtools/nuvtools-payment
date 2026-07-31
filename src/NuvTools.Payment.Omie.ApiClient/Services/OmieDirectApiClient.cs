using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NuvTools.Common.ResultWrapper;
using NuvTools.Payment.Omie.ApiClient.Configuration;
using NuvTools.Payment.Omie.ApiClient.Resources;

namespace NuvTools.Payment.Omie.ApiClient.Services;

/// <summary>
/// Raw call to the Omie API, for the registrations <see cref="Contracts.IOmieApiClient"/> does not cover or covers
/// without the fields a caller needs. It concentrates three details that any new call would otherwise repeat:
/// <list type="number">
/// <item>the <c>Content-Type</c> goes <b>without</b> the <c>charset</c> parameter — with it, the Omie gateway
/// answers a SOAP "Bad Request" envelope (and that is exactly what <c>PostAsJsonAsync</c>/<c>JsonContent</c> would
/// send);</item>
/// <item>business errors come in the body (<c>faultstring</c>) alongside an HTTP 5xx — without reading it, the
/// failure turns into an "InternalServerError" that says nothing;</item>
/// <item>consumption refusals (<c>REDUNDANT</c>, <c>MISUSE_API_PROCESS</c>) are not request errors and get their own
/// treatment (see <see cref="OmieFaultClassifier.IsThrottled(string)"/>).</item>
/// </list>
/// </summary>
public class OmieDirectApiClient(
    HttpClient httpClient,
    IOptions<OmieApiClientConfig> optionsAccessor,
    ILogger<OmieDirectApiClient> logger)
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { PropertyNamingPolicy = null };

    private readonly OmieApiClientConfig _options = optionsAccessor.Value;

    /// <summary>The same configuration the typed client uses — credentials, base URL and endpoint paths.</summary>
    public OmieApiClientConfig Options => _options;

    /// <summary>Runs an Omie call (<c>call</c> + <c>param</c>) and returns the deserialized response.</summary>
    public async Task<IResult<TResponse>> CallAsync<TResponse>(
        string endpoint, string call, object param, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.AppKey) || string.IsNullOrWhiteSpace(_options.AppSecret)
            || string.IsNullOrWhiteSpace(_options.BaseUrl) || string.IsNullOrWhiteSpace(endpoint))
            return Result<TResponse>.Fail(Messages.OmieIntegrationNotConfigured);

        var url = _options.ResolveUrl(endpoint);

        var request = new
        {
            call,
            app_key = _options.AppKey,
            app_secret = _options.AppSecret,
            param = new[] { param }
        };

        try
        {
            using var content = new StringContent(JsonSerializer.Serialize(request, SerializerOptions), Encoding.UTF8, "application/json");
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            using var httpResponse = await httpClient.PostAsync(url, content, cancellationToken);
            var body = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

            if (!httpResponse.IsSuccessStatusCode)
            {
                var fault = DescribeFault(body);

                if (OmieFaultClassifier.IsThrottled(fault))
                {
                    logger.LogWarning("Omie throttled {Call}. Url {Url}: {Fault}", call, url, fault);
                    return Result<TResponse>.Fail(OmieFaultClassifier.DescribeThrottle(fault));
                }

                logger.LogError("Omie {Call} failed. Url {Url}, Status {Status}, Body {Body}",
                    call, url, (int)httpResponse.StatusCode, Truncate(body, 1000));

                return Result<TResponse>.Fail(string.Format(Messages.OmieCallFailedXStatusXFaultX,
                    call, (int)httpResponse.StatusCode, fault ?? Truncate(body, 300)));
            }

            var data = JsonSerializer.Deserialize<TResponse>(body, SerializerOptions);

            return data is null
                ? Result<TResponse>.Fail(string.Format(Messages.OmieReturnedEmptyResponseX, call))
                : Result<TResponse>.Success(data);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogError(ex, "Failed calling Omie {Call}. Url {Url}", call, url);
            return Result<TResponse>.Fail(string.Format(Messages.OmieCallFailedXReasonX, call, ex.Message));
        }
    }

    /// <summary>
    /// "Não existem registros para a página [n]" — how Omie signals the end of pagination (and an empty
    /// registration). It comes as an error, so whoever paginates has to tell it apart from a real failure.
    /// </summary>
    public static bool IsEndOfPages(string? message)
        => message is not null && message.Contains("existem registros", StringComparison.OrdinalIgnoreCase);

    private static string? DescribeFault(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;

        try
        {
            var fault = JsonSerializer.Deserialize<OmieFault>(body, SerializerOptions)?.FaultString;
            return string.IsNullOrWhiteSpace(fault) ? null : fault;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string Truncate(string value, int max)
    {
        var clean = value.Replace('\n', ' ').Replace('\r', ' ').Trim();
        return clean.Length <= max ? clean : clean[..max] + "…";
    }

    private sealed class OmieFault
    {
        [JsonPropertyName("faultstring")] public string? FaultString { get; set; }
    }
}
