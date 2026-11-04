using System.Net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NuvTools.Payment.Omie.ApiClient.Configuration;
using NuvTools.Payment.Omie.ApiClient.Services;

namespace NuvTools.Payment.Omie.ApiClient.Tests;

public class OmieServiceCatalogProviderTests
{
    private const long ServiceCode = 555;

    private static string Registration(string taxClassification) => $$$"""
        {"cabecalho":{"cCodigo":"SRV01","cDescricao":"Registro de contrato","nPrecoUnit":10.5,
          "cCodLC116":"1.03","cCodServMun":"0103"},
         "impostos":{"cClassTrib":"{{{taxClassification}}}"}}
        """;

    private const string RedundantFault =
        """{"faultstring":"ERROR: Consumo redundante detectado. Aguarde 42 segundos.","faultcode":"SOAP-ENV:Client-6"}""";

    private static (OmieServiceCatalogProvider Provider, SequenceHttpMessageHandler Handler, ManualTimeProvider Clock)
        Create(params (HttpStatusCode, string)[] responses)
    {
        var config = new OmieApiClientConfig
        {
            AppKey = "k",
            AppSecret = "s",
            BaseUrl = "https://omie.test/",
            MaxRetryAttempts = 0
        };
        var handler = new SequenceHttpMessageHandler(responses);
        var client = new OmieApiClient(Options.Create(config), NullLogger<OmieApiClient>.Instance, handler);
        var clock = new ManualTimeProvider(new DateTimeOffset(2026, 11, 3, 20, 0, 0, TimeSpan.FromHours(-3)));
        var provider = new OmieServiceCatalogProvider(client, new MemoryCache(new MemoryCacheOptions()), clock);
        return (provider, handler, clock);
    }

    [Fact]
    public async Task GetServiceDetail_WithinMaxAge_ReusesTheRead()
    {
        var (provider, handler, clock) = Create((HttpStatusCode.OK, Registration("000001")));

        await provider.GetServiceDetailAsync(ServiceCode, TimeSpan.FromMinutes(1), CancellationToken.None);
        clock.Advance(TimeSpan.FromSeconds(30));
        var second = await provider.GetServiceDetailAsync(ServiceCode, TimeSpan.FromMinutes(1), CancellationToken.None);

        Assert.True(second.Succeeded);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task GetServiceDetail_OlderThanMaxAge_ReReadsTheEditedRegistration()
    {
        var (provider, handler, clock) = Create(
            (HttpStatusCode.OK, Registration("000001")),
            (HttpStatusCode.OK, Registration("410999")));

        var before = await provider.GetServiceDetailAsync(ServiceCode, CancellationToken.None);
        clock.Advance(TimeSpan.FromMinutes(11));
        var forIssuing = await provider.GetServiceDetailAsync(ServiceCode, TimeSpan.FromMinutes(1), CancellationToken.None);

        Assert.Equal("000001", before.Data!.TaxClassification);
        Assert.Equal("410999", forIssuing.Data!.TaxClassification);
        Assert.Equal(2, handler.CallCount);
    }

    [Fact]
    public async Task GetServiceDetail_DefaultOverload_KeepsTheLongCache()
    {
        var (provider, handler, clock) = Create(
            (HttpStatusCode.OK, Registration("000001")),
            (HttpStatusCode.OK, Registration("410999")));

        await provider.GetServiceDetailAsync(ServiceCode, CancellationToken.None);
        clock.Advance(TimeSpan.FromMinutes(11));
        var again = await provider.GetServiceDetailAsync(ServiceCode, CancellationToken.None);

        Assert.Equal("000001", again.Data!.TaxClassification);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task GetServiceDetail_ReReadRefusedAsRedundant_FailsInsteadOfServingTheStaleCopy()
    {
        var (provider, _, clock) = Create(
            (HttpStatusCode.OK, Registration("000001")),
            (HttpStatusCode.InternalServerError, RedundantFault));

        await provider.GetServiceDetailAsync(ServiceCode, CancellationToken.None);
        clock.Advance(TimeSpan.FromMinutes(5));
        var result = await provider.GetServiceDetailAsync(ServiceCode, TimeSpan.FromMinutes(1), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.True(OmieFaultClassifier.IsThrottled(result));
    }

    [Fact]
    public async Task GetServiceDetail_FreshRead_RefreshesTheCacheForTheDefaultOverload()
    {
        var (provider, handler, clock) = Create(
            (HttpStatusCode.OK, Registration("000001")),
            (HttpStatusCode.OK, Registration("410999")));

        await provider.GetServiceDetailAsync(ServiceCode, CancellationToken.None);
        clock.Advance(TimeSpan.FromMinutes(5));
        await provider.GetServiceDetailAsync(ServiceCode, TimeSpan.FromMinutes(1), CancellationToken.None);
        var screen = await provider.GetServiceDetailAsync(ServiceCode, CancellationToken.None);

        Assert.Equal("410999", screen.Data!.TaxClassification);
        Assert.Equal(2, handler.CallCount);
    }
}
