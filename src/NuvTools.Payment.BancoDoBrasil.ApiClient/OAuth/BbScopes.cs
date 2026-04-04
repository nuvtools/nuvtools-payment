namespace NuvTools.Payment.BancoDoBrasil.ApiClient.OAuth;

/// <summary>
/// Constantes de scopes OAuth2 da API Banco do Brasil.
/// </summary>
public static class BbScopes
{
    public const string BankSlipPayment = "pagamentos-lote-boletos";
    public const string BankSlipPaymentRead = "pagamentos-lote-boletos.read";
    public const string BankSlipPaymentWrite = "pagamentos-lote-boletos.write";
}
