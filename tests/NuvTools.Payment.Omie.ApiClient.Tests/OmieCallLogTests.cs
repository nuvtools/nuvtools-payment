using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NuvTools.Payment.Omie.ApiClient.Configuration;
using NuvTools.Payment.Omie.ApiClient.Services;

namespace NuvTools.Payment.Omie.ApiClient.Tests;

public class OmieCallLogTests
{
    private const string AppKey = "4471862012345";
    private const string AppSecret = "a1b2c3d4e5f6secret";

    [Fact]
    public void MaskCredentials_ReplacesKeyAndSecret_KeepsTheRestVerbatim()
    {
        var json = $$"""{"call":"ConsultarOS","param":[{"nCodOS":null,"cCodIntOS":"RGC-202610-31-06043050000132"}],"app_key":"{{AppKey}}","app_secret":"{{AppSecret}}"}""";

        var masked = OmieCallLog.MaskCredentials(json);

        Assert.Equal(
            """{"call":"ConsultarOS","param":[{"nCodOS":null,"cCodIntOS":"RGC-202610-31-06043050000132"}],"app_key":"***","app_secret":"***"}""",
            masked);
    }

    [Fact]
    public void MaskCredentials_HandlesSpacingAndEscapedQuotes()
    {
        var json = """{ "call" : "IncluirOS", "app_key" : "k\"ey", "app_secret": "s\\e\"c" }""";

        var masked = OmieCallLog.MaskCredentials(json);

        Assert.Equal("""{ "call" : "IncluirOS", "app_key" : "***", "app_secret": "***" }""", masked);
    }

    [Fact]
    public void CallOf_ReadsTheEnvelopeCall()
        => Assert.Equal("ConsultarOS", OmieCallLog.CallOf("""{"call":"ConsultarOS","param":[]}"""));

    [Fact]
    public void Truncate_CutsLongBodies()
    {
        var body = new string('x', OmieCallLog.MaxBodyLength + 10);

        var truncated = OmieCallLog.Truncate(body);

        Assert.Equal(OmieCallLog.MaxBodyLength + 1, truncated.Length);
        Assert.EndsWith("…", truncated);
    }

    [Fact]
    public async Task FailedHttpCall_LogsMaskedRequestAndResponse_AndCallerStillReadsTheFault()
    {
        const string fault = """{"faultstring":"ERROR: Ordem de Serviço não cadastrada para o Código de Integração [RGC-1]!","faultcode":"SOAP-ENV:Client-5113"}""";
        var logger = new CapturingLogger<OmieApiClient>();
        var client = CreateClient(HttpStatusCode.InternalServerError, fault, logger);

        var result = await client.ConsultOSAsync(null, "RGC-1");

        Assert.False(result.Succeeded);
        Assert.True(OmieFaultClassifier.IsNotFound(result));

        var entry = Assert.Single(logger.Entries, e => e.Message.StartsWith("Omie call failed"));
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Contains("Call ConsultarOS", entry.Message);
        Assert.Contains("Status 500", entry.Message);
        Assert.Contains("\"cCodIntOS\":\"RGC-1\"", entry.Message);
        Assert.Contains("\"app_key\":\"***\"", entry.Message);
        Assert.Contains("\"app_secret\":\"***\"", entry.Message);
        Assert.Contains("não cadastrada", entry.Message);
        Assert.DoesNotContain(AppKey, entry.Message);
        Assert.DoesNotContain(AppSecret, entry.Message);
    }

    [Fact]
    public async Task BusinessFaultWithHttp200_LogsMaskedRequestAndResponse()
    {
        var logger = new CapturingLogger<OmieApiClient>();
        var client = CreateClient(HttpStatusCode.OK, """{"cCodStatus":"101","cDescStatus":"Erro de negocio"}""", logger);

        var result = await client.ConsultOSAsync(987);

        Assert.False(result.Succeeded);

        var entry = Assert.Single(logger.Entries, e => e.Message.StartsWith("Omie call failed"));
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Contains("Status 200", entry.Message);
        Assert.Contains("Erro de negocio", entry.Message);
        Assert.DoesNotContain(AppSecret, entry.Message);
    }

    [Fact]
    public async Task SuccessfulCall_LogsNothingAboutTheExchange()
    {
        var logger = new CapturingLogger<OmieApiClient>();
        var client = CreateClient(HttpStatusCode.OK, """{"cabecalho":{"nCodOS":987,"cCodIntOS":"abc","cEtapa":"20"}}""", logger);

        var result = await client.ConsultOSAsync(987);

        Assert.True(result.Succeeded);
        Assert.DoesNotContain(logger.Entries, e => e.Message.StartsWith("Omie call failed"));
    }

    private static OmieApiClient CreateClient(HttpStatusCode status, string body, ILogger<OmieApiClient> logger)
    {
        var config = new OmieApiClientConfig
        {
            AppKey = AppKey,
            AppSecret = AppSecret,
            BaseUrl = "https://omie.test/",
            MaxRetryAttempts = 0
        };

        return new OmieApiClient(Options.Create(config), logger, new StubHttpMessageHandler(status, body));
    }
}
