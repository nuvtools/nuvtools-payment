using NuvTools.Common.ResultWrapper;
using NuvTools.Payment.BancoDoBrasil.ApiClient.DTOs.Requests;
using NuvTools.Payment.BancoDoBrasil.ApiClient.DTOs.Responses;

namespace NuvTools.Payment.BancoDoBrasil.ApiClient.Contracts;

/// <summary>
/// Cliente da API "Pagamentos em Lote" do Banco do Brasil, seção de pagamento de guias com código de
/// barras (guias de arrecadação — ex.: taxa do Detran). Fluxo: obter número de requisição → criar o lote
/// (<c>/lotes-guias-codigo-barras</c>) → liberar (<c>/liberar-pagamentos</c>) → consultar a situação do
/// lançamento (<c>/guias-codigo-barras/{idLancamento}</c>).
/// </summary>
public interface IBbBankSlipPaymentApiClient
{
    Task<IResult<BbAccessTokenResponse>> GenerateAccessTokenAsync(string scope, CancellationToken cancellationToken = default);

    /// <summary>Obtém números de requisição disponíveis (<c>GET /proximos-numeros-requisicao</c>).</summary>
    Task<IResult<NextRequisitionNumbersResponse>> GetNextRequisitionNumbersAsync(int quantity, CancellationToken cancellationToken = default);

    /// <summary>Cria o lote de pagamento de guias (<c>POST /lotes-guias-codigo-barras</c>).</summary>
    Task<IResult<GuiaBatchPaymentResponse>> CreateGuiaBatchPaymentAsync(GuiaBatchPaymentRequest request, CancellationToken cancellationToken = default);

    /// <summary>Libera (autoriza) a requisição de pagamento (<c>POST /liberar-pagamentos</c>).</summary>
    Task<IResult<ReleasePaymentResponse>> ReleasePaymentAsync(ReleasePaymentRequest request, CancellationToken cancellationToken = default);

    /// <summary>Consulta a situação de um lançamento de guia (<c>GET /guias-codigo-barras/{idLancamento}</c>).</summary>
    Task<IResult<GuiaPaymentResponse>> GetGuiaPaymentAsync(long idLancamento, int agencia, long contaCorrente, string digitoVerificador, CancellationToken cancellationToken = default);

    /// <summary>
    /// Consulta uma requisição de guias já criada (<c>GET /lotes-guias-codigo-barras/{idRequisicao}/solicitacao</c>).
    /// Usada para recuperar o lançamento de forma idempotente quando a criação retorna "número já utilizado".
    /// </summary>
    Task<IResult<GuiaBatchPaymentResponse>> GetGuiaRequisitionAsync(long idRequisicao, int agencia, long contaCorrente, string digitoVerificador, CancellationToken cancellationToken = default);
}
