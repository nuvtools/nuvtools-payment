using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NuvTools.Payment.Omie.ApiClient.Configuration;
using NuvTools.Payment.Omie.ApiClient.DTOs.Responses;
using NuvTools.Payment.Omie.ApiClient.Services;

namespace NuvTools.Payment.Omie.ApiClient.Tests;

/// <summary>
/// Picking the service order category (<c>cCodCateg</c>) by name. The real case behind the test: the default Omie
/// chart of accounts has no "Registro de Contrato", and the right account is called "Clientes - Serviços Prestados"
/// — the preference has to fall through to the next one and match by containing the term, with different accents
/// and casing.
/// </summary>
public class OmieCategoryMatchTests
{
    private static readonly string[] DefaultPreferences =
        ["Registro de Contrato", "Serviços Prestados", "Prestação de Serviços", "Receita de Serviços"];

    /// <summary>Usable revenue accounts, like the ones of the default Omie chart of accounts.</summary>
    private static List<OmieCategory> RealWorldPlan() =>
    [
        new("1.01.01", "Clientes - Venda de Mercadoria Fabricadas"),
        new("1.01.02", "Clientes - Serviços Prestados"),
        new("1.01.03", "Clientes - Revenda de Mercadoria"),
        new("1.03.26", "Devoluções de Compra de Serviços"),
        new("1.04.02", "Reembolso de Despesas")
    ];

    [Fact]
    public void Match_Falls_Through_To_Next_Preference_And_Matches_By_Contains()
    {
        var match = OmieCategoryProvider.Match(RealWorldPlan(), DefaultPreferences);

        Assert.NotNull(match);
        Assert.Equal("1.01.02", match!.Code);
    }

    [Fact]
    public void Match_Prefers_Exact_Name_Over_Contains()
    {
        var plan = RealWorldPlan();
        plan.Add(new OmieCategory("1.09.99", "Serviços Prestados"));

        var match = OmieCategoryProvider.Match(plan, DefaultPreferences);

        Assert.Equal("1.09.99", match!.Code);
    }

    [Fact]
    public void Match_Honours_Preference_Order()
    {
        var plan = RealWorldPlan();
        plan.Add(new OmieCategory("1.08.01", "Registro de Contrato"));

        var match = OmieCategoryProvider.Match(plan, DefaultPreferences);

        Assert.Equal("1.08.01", match!.Code);
    }

    [Fact]
    public void Match_Ignores_Accents_And_Case()
    {
        List<OmieCategory> plan = [new("1.02.03", "CLIENTES - SERVICOS PRESTADOS")];

        var match = OmieCategoryProvider.Match(plan, DefaultPreferences);

        Assert.Equal("1.02.03", match!.Code);
    }

    /// <summary>
    /// With no match, "the closest one" is not picked: invoicing on the wrong account is worse than not invoicing,
    /// and the caller hands the category list back for the user to choose from.
    /// </summary>
    [Fact]
    public void Match_Returns_Null_When_Nothing_Corresponds()
    {
        List<OmieCategory> plan =
        [
            new("1.01.01", "Clientes - Venda de Mercadoria Fabricadas"),
            new("1.02.01", "Dividendos Recebidos")
        ];

        Assert.Null(OmieCategoryProvider.Match(plan, DefaultPreferences));
    }

    /// <summary>
    /// The configured code has to come out in <c>Data</c>, not in <c>Message</c>. On <c>Result&lt;string&gt;</c> the
    /// positional <c>Success</c> overload is the message one: calling it without <c>data:</c> returns success with a
    /// null <c>Data</c>, and the category reaches Omie blank — which refuses the service order for a missing
    /// required field, without saying which side got it wrong. That is exactly what happened once.
    /// </summary>
    [Fact]
    public async Task ResolveCategoryCode_Returns_Configured_Code_As_Data()
    {
        var config = new OmieApiClientConfig { AppKey = "k", AppSecret = "s", BaseUrl = "https://omie.test/" };

        var provider = new OmieCategoryProvider(
            new OmieDirectApiClient(new HttpClient(), Options.Create(config),
                NullLogger<OmieDirectApiClient>.Instance),
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger<OmieCategoryProvider>.Instance);

        // With the code configured, resolution returns before any call to Omie.
        var result = await provider.ResolveCategoryCodeAsync("1.01.02", [], CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("1.01.02", result.Data);
    }
}
