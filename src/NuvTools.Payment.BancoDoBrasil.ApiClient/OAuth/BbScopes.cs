namespace NuvTools.Payment.BancoDoBrasil.ApiClient.OAuth;

/// <summary>
/// Escopos OAuth2 da API "Pagamentos em Lote" do Banco do Brasil (fluxo client_credentials).
/// O cliente opera sobre guias com código de barras, então o token precisa dos escopos de guia
/// (requisição + consulta de guias e lotes) — ver <see cref="GuiaPayment"/>.
/// </summary>
public static class BbScopes
{
    public const string BoletoRequisicao = "pagamentos-lote.boletos-requisicao";
    public const string BoletoInfo = "pagamentos-lote.boletos-info";
    public const string LoteRequisicao = "pagamentos-lote.lotes-requisicao";
    public const string LoteInfo = "pagamentos-lote.lotes-info";
    public const string GuiaRequisicao = "pagamentos-lote.guias-codigo-barras-requisicao";
    public const string GuiaInfo = "pagamentos-lote.guias-codigo-barras-info";

    /// <summary>
    /// Conjunto de escopos para pagamento de guias com código de barras (enviado no token client_credentials).
    /// É o usado pelo cliente: os endpoints são <c>lotes-guias-codigo-barras</c> / <c>guias-codigo-barras/{id}</c>.
    /// </summary>
    public const string GuiaPayment = GuiaRequisicao + " " + GuiaInfo + " " + LoteRequisicao + " " + LoteInfo;

    /// <summary>Conjunto de escopos de boleto (mantido para uso futuro de boletos de cobrança).</summary>
    public const string BoletoPayment = BoletoRequisicao + " " + BoletoInfo + " " + LoteRequisicao + " " + LoteInfo;
}
