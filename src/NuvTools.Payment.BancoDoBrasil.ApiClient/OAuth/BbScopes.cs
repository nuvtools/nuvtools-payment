namespace NuvTools.Payment.BancoDoBrasil.ApiClient.OAuth;

/// <summary>
/// Escopos OAuth2 da API "Pagamentos em Lote" do Banco do Brasil (fluxo client_credentials).
/// O token de pagamento de boletos precisa do conjunto abaixo (requisição + consulta de boletos e lotes).
/// </summary>
public static class BbScopes
{
    public const string BoletoRequisicao = "pagamentos-lote.boletos-requisicao";
    public const string BoletoInfo = "pagamentos-lote.boletos-info";
    public const string LoteRequisicao = "pagamentos-lote.lotes-requisicao";
    public const string LoteInfo = "pagamentos-lote.lotes-info";

    /// <summary>Conjunto de escopos usado no pagamento de boletos (enviado no token client_credentials).</summary>
    public const string BoletoPayment = BoletoRequisicao + " " + BoletoInfo + " " + LoteRequisicao + " " + LoteInfo;
}
