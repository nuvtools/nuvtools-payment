using System.Text.Json;
using NuvTools.Payment.Omie.ApiClient.DTOs.Requests;
using NuvTools.Payment.Omie.ApiClient.DTOs.Responses;

namespace NuvTools.Payment.Omie.ApiClient.Tests;

/// <summary>
/// The service order item tax block built from the service registration. The real case behind the test: an OS item
/// sent with only <c>cClassTrib</c> arrived in Omie with the Reforma Tributária tab (CST, indicador de operação,
/// IBS/CBS) and the federal retentions empty, although the service registration had all of them.
/// </summary>
public class IncludeOSTaxesFromRegistrationTests
{
    /// <summary>The registration "DETRAN MG - TAXA REGISTRADORA COM RETENÇÃO", as ConsultarCadastroServico returns it.</summary>
    private const string RegistrationTaxesJson = """
        {
          "nAliqISS": 2, "cRetISS": "N",
          "nAliqPIS": 0.65, "cRetPIS": "S", "nRedBasePIS": 0,
          "nAliqCOFINS": 3, "cRetCOFINS": "S", "nRedBaseCOFINS": 0,
          "nAliqCSLL": 1, "cRetCSLL": "S",
          "nAliqIR": 1.5, "cRetIR": "S",
          "nAliqINSS": 0, "cRetINSS": "N", "nRedBaseINSS": 0,
          "lDeduzISS": false,
          "cCstIbsCbs": "000", "cClassTrib": "000001", "cIndOper": "050101",
          "nAliqIbsMun": 0, "nAliqIbsUf": 0.1, "nAliqCbs": 0.9,
          "nPercReducaoIbsMun": 0, "nPercReducaoIbsUf": 0, "nPercReducaoCbs": 0,
          "cCstPIS": "", "cCstCOFINS": ""
        }
        """;

    [Fact]
    public void FromServiceRegistration_Carries_Every_Tax_Field_Under_The_OS_Tag_Names()
    {
        var registration = JsonSerializer.Deserialize<ConsultServiceTaxes>(RegistrationTaxesJson)!;

        var taxes = IncludeOSTaxes.FromServiceRegistration(registration);
        var json = JsonSerializer.SerializeToElement(taxes);

        // Reforma Tributária
        Assert.Equal("000", json.GetProperty("cCST").GetString());
        Assert.Equal("000001", json.GetProperty("cClassTrib").GetString());
        Assert.Equal("050101", json.GetProperty("cIndOPer").GetString());
        Assert.Equal(0m, json.GetProperty("nAliqIbsMun").GetDecimal());
        Assert.Equal(0.1m, json.GetProperty("nAliqIbsUf").GetDecimal());
        Assert.Equal(0.9m, json.GetProperty("nAliqCbs").GetDecimal());

        // ISS, PIS, COFINS, CSLL, IR, INSS
        Assert.Equal(2m, json.GetProperty("nAliqISS").GetDecimal());
        Assert.Equal("S", json.GetProperty("cRetemPIS").GetString());
        Assert.Equal(0.65m, json.GetProperty("nAliqPIS").GetDecimal());
        Assert.Equal("S", json.GetProperty("cRetemCOFINS").GetString());
        Assert.Equal(3m, json.GetProperty("nAliqCOFINS").GetDecimal());
        Assert.Equal("S", json.GetProperty("cRetemCSLL").GetString());
        Assert.Equal(1m, json.GetProperty("nAliqCSLL").GetDecimal());
        Assert.Equal("S", json.GetProperty("cRetemIRRF").GetString());
        Assert.Equal(1.5m, json.GetProperty("nAliqIRRF").GetDecimal());
        Assert.Equal("N", json.GetProperty("cRetemINSS").GetString());
        Assert.Equal(0m, json.GetProperty("nAliqINSS").GetDecimal());
        Assert.False(json.GetProperty("lDeduzISS").GetBoolean());
    }

    [Fact]
    public void FromServiceRegistration_Leaves_Blank_Codes_Null_And_Read_Only_Tags_Unset()
    {
        var taxes = IncludeOSTaxes.FromServiceRegistration(new ConsultServiceTaxes
        {
            IbsCbsCst = " ",
            TaxClassification = "",
            OperationIndicator = null,
            WithholdPis = ""
        });

        Assert.Null(taxes.IbsCbsCst);
        Assert.Null(taxes.TaxClassification);
        Assert.Null(taxes.OperationIndicator);
        Assert.Null(taxes.WithholdPis);

        // Calculated or "manual change" markers are Omie's to fill — never sent.
        Assert.Null(taxes.IssValue);
        Assert.Null(taxes.IssBase);
        Assert.Null(taxes.FixIss);
        Assert.Null(taxes.ManualCbs);
        Assert.Null(taxes.CbsValue);
    }
}
