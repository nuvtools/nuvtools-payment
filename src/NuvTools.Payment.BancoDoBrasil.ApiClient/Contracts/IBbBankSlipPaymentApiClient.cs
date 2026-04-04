using NuvTools.Payment.BancoDoBrasil.ApiClient.DTOs.Requests;
using NuvTools.Payment.BancoDoBrasil.ApiClient.DTOs.Responses;

namespace NuvTools.Payment.BancoDoBrasil.ApiClient.Contracts;

/// <summary>
/// Interface para comunicacao com a API de pagamentos de boleto do Banco do Brasil.
/// </summary>
public interface IBbBankSlipPaymentApiClient
{
    /// <summary>
    /// Gera um token de acesso OAuth2 via client_credentials.
    /// </summary>
    Task<BbApiResult<BbAccessTokenResponse>> GenerateAccessTokenAsync(string scope, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cria um lote de pagamentos de boleto.
    /// </summary>
    Task<BbApiResult<CreateBatchPaymentResponse>> CreateBatchPaymentAsync(CreateBatchPaymentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Consulta um pagamento pelo ID.
    /// </summary>
    Task<BbApiResult<BankSlipPaymentResponse>> GetPaymentAsync(long paymentId, string agency, string account, string digit, CancellationToken cancellationToken = default);
}
