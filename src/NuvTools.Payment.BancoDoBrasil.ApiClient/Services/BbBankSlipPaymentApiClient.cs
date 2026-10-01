using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NuvTools.Common.ResultWrapper;
using NuvTools.Payment.BancoDoBrasil.ApiClient.Configuration;
using NuvTools.Payment.BancoDoBrasil.ApiClient.Contracts;
using NuvTools.Payment.BancoDoBrasil.ApiClient.DTOs.Requests;
using NuvTools.Payment.BancoDoBrasil.ApiClient.DTOs.Responses;

namespace NuvTools.Payment.BancoDoBrasil.ApiClient.Services;

/// <summary>
/// Implementação da API "Pagamentos em Lote" (guias com código de barras) do Banco do Brasil. A app-key vai
/// como parâmetro de query (<c>gw-dev-app-key</c>) e o Bearer é anexado pelo <see cref="OAuth.BbAuthHandler"/>.
/// A URL base (<c>BaseUrl</c>) já inclui o caminho do serviço (ex.: <c>.../pagamentos-lote/v1</c>).
/// </summary>
public class BbBankSlipPaymentApiClient(
    HttpClient httpClient,
    IOptions<BancoDoBrasilApiClientConfig> options,
    ILogger<BbBankSlipPaymentApiClient> logger) : IBbBankSlipPaymentApiClient
{
    private readonly BancoDoBrasilApiClientConfig _config = options.Value;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        // O BB devolve alguns numéricos como string (ex.: codigoIdentificadorPagamento = "90024247731030001"
        // na criação do lote, enquanto o id vem como número na consulta). Permite ler ambos.
        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
    };

    public async Task<IResult<BbAccessTokenResponse>> GenerateAccessTokenAsync(string scope, CancellationToken cancellationToken = default)
    {
        try
        {
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_config.ClientId}:{_config.ClientSecret}"));

            using var request = new HttpRequestMessage(HttpMethod.Post, _config.AuthUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["scope"] = scope
            });

            var response = await httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                return Result<BbAccessTokenResponse>.Fail($"Erro ao gerar token BB: {response.StatusCode} - {responseBody}", logger: logger);

            var result = JsonSerializer.Deserialize<BbAccessTokenResponse>(responseBody, JsonOptions);

            return result?.AccessToken != null
                ? Result<BbAccessTokenResponse>.Success(result)
                : Result<BbAccessTokenResponse>.Fail("Resposta invalida da API BB (GenerateAccessToken).", logger: logger);
        }
        catch (Exception ex)
        {
            return Result<BbAccessTokenResponse>.Fail(ex, logger: logger);
        }
    }

    public Task<IResult<NextRequisitionNumbersResponse>> GetNextRequisitionNumbersAsync(int quantity, CancellationToken cancellationToken = default)
        => GetAsync<NextRequisitionNumbersResponse>(
            $"proximos-numeros-requisicao?{AppKey()}&quantidadeProximosNumeros={quantity}", cancellationToken);

    public Task<IResult<GuiaBatchPaymentResponse>> CreateGuiaBatchPaymentAsync(GuiaBatchPaymentRequest request, CancellationToken cancellationToken = default)
        => PostAsync<GuiaBatchPaymentRequest, GuiaBatchPaymentResponse>($"lotes-guias-codigo-barras?{AppKey()}", request, cancellationToken);

    public Task<IResult<ReleasePaymentResponse>> ReleasePaymentAsync(ReleasePaymentRequest request, CancellationToken cancellationToken = default)
        => PostAsync<ReleasePaymentRequest, ReleasePaymentResponse>($"liberar-pagamentos?{AppKey()}", request, cancellationToken);

    public Task<IResult<GuiaPaymentResponse>> GetGuiaPaymentAsync(long idLancamento, int agencia, long contaCorrente, string digitoVerificador, CancellationToken cancellationToken = default)
        => GetAsync<GuiaPaymentResponse>(
            $"guias-codigo-barras/{idLancamento}?{AppKey()}&agencia={agencia}&contaCorrente={contaCorrente}&digitoVerificador={Uri.EscapeDataString(digitoVerificador)}", cancellationToken);

    public Task<IResult<GuiaBatchPaymentResponse>> GetGuiaRequisitionAsync(long idRequisicao, int agencia, long contaCorrente, string digitoVerificador, CancellationToken cancellationToken = default)
        => GetAsync<GuiaBatchPaymentResponse>(
            $"lotes-guias-codigo-barras/{idRequisicao}/solicitacao?{AppKey()}&agencia={agencia}&contaCorrente={contaCorrente}&digitoVerificador={Uri.EscapeDataString(digitoVerificador)}", cancellationToken);

    private string AppKey() => $"gw-dev-app-key={Uri.EscapeDataString(_config.ApiKey)}";

    private string Url(string relative) => $"{_config.BaseUrl.TrimEnd('/')}/{relative}";

    private async Task<IResult<TResponse>> GetAsync<TResponse>(string relative, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, Url(relative));
            return await SendAsync<TResponse>(request, cancellationToken);
        }
        catch (Exception ex)
        {
            return Result<TResponse>.Fail(ex, logger: logger);
        }
    }

    private async Task<IResult<TResponse>> PostAsync<TRequest, TResponse>(string relative, TRequest body, CancellationToken cancellationToken)
    {
        try
        {
            var json = JsonSerializer.Serialize(body, JsonOptions);
            using var request = new HttpRequestMessage(HttpMethod.Post, Url(relative))
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            return await SendAsync<TResponse>(request, cancellationToken);
        }
        catch (Exception ex)
        {
            return Result<TResponse>.Fail(ex, logger: logger);
        }
    }

    private async Task<IResult<TResponse>> SendAsync<TResponse>(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            return Result<TResponse>.Fail($"Erro na API BB Pagamentos em Lote: {response.StatusCode} - {body}", logger: logger);

        var result = JsonSerializer.Deserialize<TResponse>(body, JsonOptions);
        return result is not null
            ? Result<TResponse>.Success(result)
            : Result<TResponse>.Fail("Resposta invalida da API BB (Pagamentos em Lote).", logger: logger);
    }
}
