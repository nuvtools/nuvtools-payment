using NuvTools.Payment.Omie.ApiClient.DTOs.Requests;
using NuvTools.Payment.Omie.ApiClient.DTOs.Responses;

namespace NuvTools.Payment.Omie.ApiClient.Contracts;

/// <summary>
/// Interface para comunicacao com a API Omie.
/// </summary>
public interface IOmieApiClient
{
    /// <summary>
    /// Consulta se um cliente existe no Omie pelo codigo.
    /// </summary>
    Task<OmieApiResult<bool>> ConsultarClienteAsync(long codigoClienteOmie, CancellationToken cancellationToken = default);

    /// <summary>
    /// Consulta os dados cadastrais de um servico no Omie.
    /// </summary>
    Task<OmieApiResult<ConsultarCadastroServicoResponse>> ConsultarCadastroServicoAsync(long serviceCodeOmie, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inclui uma Ordem de Servico no Omie.
    /// </summary>
    Task<OmieApiResult<IncluirOSResponse>> IncluirOSAsync(IncluirOSParam param, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fatura uma Ordem de Servico existente no Omie.
    /// </summary>
    Task<OmieApiResult<FaturarOSResponse>> FaturarOSAsync(string internalCodeOS, CancellationToken cancellationToken = default);
}
