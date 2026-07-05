using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NuvTools.Payment.Omie.ApiClient.Configuration;
using NuvTools.Payment.Omie.ApiClient.DTOs.Requests;
using NuvTools.Payment.Omie.ApiClient.Services;

namespace NuvTools.Payment.Omie.ApiClient.Tests;

public class OmieApiClientTests
{
    private static OmieApiClient CreateClient(HttpStatusCode status, string body)
    {
        var config = new OmieApiClientConfig
        {
            AppKey = "k",
            AppSecret = "s",
            BaseUrl = "https://omie.test/",
            MaxRetryAttempts = 0
        };
        var handler = new StubHttpMessageHandler(status, body);
        return new OmieApiClient(Options.Create(config), NullLogger<OmieApiClient>.Instance, handler);
    }

    private static IncludeOSParam SampleOsParam() => new()
    {
        Header = new IncludeOSHeader { OsIntegrationCode = "abc-2025-01", PaymentTermCode = "000", Stage = "10", ForecastDate = "31/01/2025", ClientCode = 1, InstallmentCount = 1 },
        AdditionalInfo = new IncludeOSAdditionalInfo { CategoryCode = "1.01.01", InvoiceAdditionalData = "x", CheckingAccountCode = 1 },
        Installments = [new IncludeOSInstallment { InstallmentNumber = 1, Days = 0, DueDate = "31/01/2025", Percentage = 100m }],
        ProvidedServices = [new IncludeOSProvidedService { ServiceCode = 100, Quantity = 5, ItemSequence = 1 }]
    };

    [Fact]
    public async Task IncludeOS_Success()
    {
        var client = CreateClient(HttpStatusCode.OK, """{"cCodIntOS":"abc-2025-01","nCodOS":987,"cNumOS":"1","cCodStatus":"0"}""");
        var result = await client.IncludeOSAsync(SampleOsParam());
        Assert.True(result.Succeeded);
        Assert.Equal(987, result.Data!.OsCode);
    }

    [Fact]
    public async Task IncludeOS_BusinessFault_Fails()
    {
        var client = CreateClient(HttpStatusCode.OK, """{"cCodStatus":"101","cDescStatus":"Erro de negocio"}""");
        var result = await client.IncludeOSAsync(SampleOsParam());
        Assert.False(result.Succeeded);
        Assert.Contains("Erro de negocio", result.Message);
    }

    [Fact]
    public async Task IncludeOS_DuplicateIntegrationCode_IsClassified()
    {
        var client = CreateClient(HttpStatusCode.InternalServerError,
            """{"faultstring":"O código de integração [abc] já foi cadastrado anteriormente.","faultcode":"SOAP-ENV:Client-101"}""");
        var result = await client.IncludeOSAsync(SampleOsParam());
        Assert.False(result.Succeeded);
        Assert.True(OmieFaultClassifier.IsDuplicateIntegrationCode(result));
    }

    [Fact]
    public async Task ChangeOS_Success()
    {
        var client = CreateClient(HttpStatusCode.OK, """{"nCodOS":987,"cCodStatus":"0"}""");
        var result = await client.ChangeOSAsync(SampleOsParam());
        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task ConsultOS_Success_ParsesHeaderStage()
    {
        var client = CreateClient(HttpStatusCode.OK, """{"cabecalho":{"nCodOS":987,"cCodIntOS":"abc","cEtapa":"20"}}""");
        var result = await client.ConsultOSAsync(987);
        Assert.True(result.Succeeded);
        Assert.Equal("20", result.Data!.Header!.Stage);
    }

    [Fact]
    public async Task ConsultOS_NotFound_IsClassified()
    {
        var client = CreateClient(HttpStatusCode.InternalServerError,
            """{"faultstring":"OS não encontrada.","faultcode":"SOAP-ENV:Client-5001"}""");
        var result = await client.ConsultOSAsync(1);
        Assert.False(result.Succeeded);
        Assert.True(OmieFaultClassifier.IsNotFound(result));
    }

    [Fact]
    public async Task ConsultOS_RequiresIdentifier()
    {
        var client = CreateClient(HttpStatusCode.OK, "{}");
        var result = await client.ConsultOSAsync();
        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task ConsultContract_Success_ParsesItemPrice()
    {
        var client = CreateClient(HttpStatusCode.OK,
            """{"cabecalho":{"nCodCtr":55},"itensContrato":[{"itemCabecalho":{"codServico":100,"valorUnitario":12.5}}]}""");
        var result = await client.ConsultContractAsync(55);
        Assert.True(result.Succeeded);
        Assert.Equal(12.5m, result.Data!.Items![0].Header!.UnitValue);
    }

    [Fact]
    public async Task ListServiceRegistration_Success()
    {
        var client = CreateClient(HttpStatusCode.OK, """{"pagina":1,"total_de_paginas":1,"cadastros":[{"intListar":{"nCodServ":100}}]}""");
        var result = await client.ListServiceRegistrationAsync();
        Assert.True(result.Succeeded);
        Assert.Equal(100, result.Data!.Services![0].IntList!.ServiceCode);
    }

    [Fact]
    public async Task ChangeOSStage_Success()
    {
        var client = CreateClient(HttpStatusCode.OK, """{"cCodStatus":"0"}""");
        var result = await client.ChangeOSStageAsync(new ChangeOSStageParam { OsCode = 987, Stage = "50" });
        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task ListBillingStages_Success()
    {
        var client = CreateClient(HttpStatusCode.OK,
            """{"cadastros":[{"cCodigo":"10","cDescricao":"Aberta"},{"cCodigo":"50","cDescricao":"Faturar"}]}""");
        var result = await client.ListBillingStagesAsync();
        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Data!.Stages!.Length);
    }

    [Fact]
    public void FaultClassifier_DetectsMarkers()
    {
        Assert.True(OmieFaultClassifier.IsDuplicateIntegrationCode("O código já foi cadastrado anteriormente"));
        Assert.True(OmieFaultClassifier.IsNotFound("Registro não encontrado"));
        Assert.False(OmieFaultClassifier.IsDuplicateIntegrationCode("algum outro erro"));
    }
}
