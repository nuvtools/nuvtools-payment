using System.Text.Json.Serialization;

namespace NuvTools.Payment.Omie.ApiClient.DTOs.Requests;

/// <summary>
/// Parametro principal para inclusao de Ordem de Servico na API Omie.
/// Corresponde ao conteudo do array "param" da requisicao IncluirOS.
/// </summary>
public class IncluirOSParam
{
    [JsonPropertyName("cabecalho")]
    public required IncluirOSCabecalho Cabecalho { get; set; }

    [JsonPropertyName("informacoesAdicionais")]
    public required IncluirOSInformacoesAdicionais InformacoesAdicionais { get; set; }

    [JsonPropertyName("parcelas")]
    public required IncluirOSParcela[] Parcelas { get; set; }

    [JsonPropertyName("servicosPrestados")]
    public required IncluirOSServicoPrestado[] ServicosPrestados { get; set; }
}

public class IncluirOSCabecalho
{
    [JsonPropertyName("cCodIntOS")]
    public required string CCodIntOS { get; set; }

    [JsonPropertyName("cCodParc")]
    public required string CCodParc { get; set; }

    [JsonPropertyName("cEtapa")]
    public required string CEtapa { get; set; }

    [JsonPropertyName("dDtPrevisao")]
    public required string DDtPrevisao { get; set; }

    [JsonPropertyName("nCodCli")]
    public required long NCodCli { get; set; }

    [JsonPropertyName("nQtdeParc")]
    public required int NQtdeParc { get; set; }

    [JsonPropertyName("nValorTotal")]
    public required decimal NValorTotal { get; set; }

    [JsonPropertyName("nValorTotalImpRet")]
    public required decimal NValorTotalImpRet { get; set; }
}

public class IncluirOSInformacoesAdicionais
{
    [JsonPropertyName("cCidPrestServ")]
    public required string CCidPrestServ { get; set; }

    [JsonPropertyName("cCodCateg")]
    public required string CCodCateg { get; set; }

    [JsonPropertyName("cContato")]
    public string? CContato { get; set; }

    [JsonPropertyName("cDadosAdicNF")]
    public required string CDadosAdicNF { get; set; }

    [JsonPropertyName("nCodCC")]
    public required long NCodCC { get; set; }
}

public class IncluirOSParcela
{
    [JsonPropertyName("dDtVenc")]
    public required string DDtVenc { get; set; }

    [JsonPropertyName("nDias")]
    public required int NDias { get; set; }

    [JsonPropertyName("nParcela")]
    public required int NParcela { get; set; }

    [JsonPropertyName("nPercentual")]
    public required decimal NPercentual { get; set; }

    [JsonPropertyName("nValor")]
    public required decimal NValor { get; set; }
}

public class IncluirOSServicoPrestado
{
    [JsonPropertyName("nCodServico")]
    public required long NCodServico { get; set; }

    [JsonPropertyName("cCodServLC116")]
    public string? CCodServLC116 { get; set; }

    [JsonPropertyName("cCodServMun")]
    public string? CCodServMun { get; set; }

    [JsonPropertyName("cDescServ")]
    public string? CDescServ { get; set; }

    [JsonPropertyName("cRetemISS")]
    public required string CRetemISS { get; set; }

    [JsonPropertyName("cCodCategItem")]
    public string? CCodCategItem { get; set; }

    [JsonPropertyName("cNaoGerarFinanceiro")]
    public required string CNaoGerarFinanceiro { get; set; }

    [JsonPropertyName("cReembolso")]
    public required string CReembolso { get; set; }

    [JsonPropertyName("cTpDesconto")]
    public required string CTpDesconto { get; set; }

    [JsonPropertyName("nAliqDesconto")]
    public required decimal NAliqDesconto { get; set; }

    [JsonPropertyName("nSeqItem")]
    public required int NSeqItem { get; set; }

    [JsonPropertyName("nValorAcrescimos")]
    public required decimal NValorAcrescimos { get; set; }

    [JsonPropertyName("nValorDesconto")]
    public required decimal NValorDesconto { get; set; }

    [JsonPropertyName("nValorOutrasRetencoes")]
    public required decimal NValorOutrasRetencoes { get; set; }

    [JsonPropertyName("nQtde")]
    public required decimal NQtde { get; set; }

    [JsonPropertyName("nValUnit")]
    public required decimal NValUnit { get; set; }

    [JsonPropertyName("impostos")]
    public required IncluirOSImpostos Impostos { get; set; }
}

public class IncluirOSImpostos
{
    [JsonPropertyName("cClassTrib")]
    public string? CClassTrib { get; set; }

    [JsonPropertyName("cFixarCOFINS")]
    public required string CFixarCOFINS { get; set; }

    [JsonPropertyName("cFixarCSLL")]
    public required string CFixarCSLL { get; set; }

    [JsonPropertyName("cFixarINSS")]
    public required string CFixarINSS { get; set; }

    [JsonPropertyName("cFixarIRRF")]
    public required string CFixarIRRF { get; set; }

    [JsonPropertyName("cFixarISS")]
    public required string CFixarISS { get; set; }

    [JsonPropertyName("cFixarPIS")]
    public required string CFixarPIS { get; set; }

    [JsonPropertyName("cIndOPer")]
    public string? CIndOPer { get; set; }

    [JsonPropertyName("cManCbs")]
    public required string CManCbs { get; set; }

    [JsonPropertyName("cManIbsMun")]
    public required string CManIbsMun { get; set; }

    [JsonPropertyName("cManIbsUf")]
    public required string CManIbsUf { get; set; }

    [JsonPropertyName("cRetemCOFINS")]
    public required string CRetemCOFINS { get; set; }

    [JsonPropertyName("cRetemCSLL")]
    public required string CRetemCSLL { get; set; }

    [JsonPropertyName("cRetemINSS")]
    public required string CRetemINSS { get; set; }

    [JsonPropertyName("cRetemIRRF")]
    public required string CRetemIRRF { get; set; }

    [JsonPropertyName("cRetemPIS")]
    public required string CRetemPIS { get; set; }

    [JsonPropertyName("lDeduzISS")]
    public required bool LDeduzISS { get; set; }

    [JsonPropertyName("nAliqCOFINS")]
    public required decimal NAliqCOFINS { get; set; }

    [JsonPropertyName("nAliqCSLL")]
    public required decimal NAliqCSLL { get; set; }

    [JsonPropertyName("nAliqCbs")]
    public required decimal NAliqCbs { get; set; }

    [JsonPropertyName("nAliqINSS")]
    public required decimal NAliqINSS { get; set; }

    [JsonPropertyName("nAliqISS")]
    public required decimal NAliqISS { get; set; }

    [JsonPropertyName("nAliqIbsMun")]
    public required decimal NAliqIbsMun { get; set; }

    [JsonPropertyName("nAliqIbsUf")]
    public required decimal NAliqIbsUf { get; set; }

    [JsonPropertyName("nAliqPIS")]
    public required decimal NAliqPIS { get; set; }

    [JsonPropertyName("nBaseISS")]
    public required decimal NBaseISS { get; set; }

    [JsonPropertyName("nPercReducaoCbs")]
    public required decimal NPercReducaoCbs { get; set; }

    [JsonPropertyName("nPercReducaoIbsMun")]
    public required decimal NPercReducaoIbsMun { get; set; }

    [JsonPropertyName("nPercReducaoIbsUf")]
    public required decimal NPercReducaoIbsUf { get; set; }

    [JsonPropertyName("nTotDeducao")]
    public required decimal NTotDeducao { get; set; }

    [JsonPropertyName("nValorCOFINS")]
    public required decimal NValorCOFINS { get; set; }

    [JsonPropertyName("nValorCSLL")]
    public required decimal NValorCSLL { get; set; }

    [JsonPropertyName("nValorCbs")]
    public required decimal NValorCbs { get; set; }

    [JsonPropertyName("nValorDeducao")]
    public required decimal NValorDeducao { get; set; }

    [JsonPropertyName("nValorDeducaoIRRF")]
    public required decimal NValorDeducaoIRRF { get; set; }

    [JsonPropertyName("nValorINSS")]
    public required decimal NValorINSS { get; set; }

    [JsonPropertyName("nValorIRRF")]
    public required decimal NValorIRRF { get; set; }

    [JsonPropertyName("nValorISS")]
    public required decimal NValorISS { get; set; }

    [JsonPropertyName("nValorIbsMun")]
    public required decimal NValorIbsMun { get; set; }

    [JsonPropertyName("nValorIbsUf")]
    public required decimal NValorIbsUf { get; set; }

    [JsonPropertyName("nValorPIS")]
    public required decimal NValorPIS { get; set; }
}
