using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NuvTools.Payment.BancoDoBrasil.ApiClient.Configuration;
using NuvTools.Payment.BancoDoBrasil.ApiClient.Contracts;
using NuvTools.Payment.BancoDoBrasil.ApiClient.DTOs.Requests;
using NuvTools.Payment.BancoDoBrasil.ApiClient.DTOs.Responses;

namespace NuvTools.Payment.BancoDoBrasil.ApiClient.Services;

/// <summary>
/// Implementacao do cliente da API de pagamentos de boleto do Banco do Brasil.
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
        PropertyNameCaseInsensitive = true
    };

    public async Task<BbApiResult<BbAccessTokenResponse>> GenerateAccessTokenAsync(string scope, CancellationToken cancellationToken = default)
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
            {
                logger.LogWarning("BB GenerateAccessToken falhou: {StatusCode} - {Body}", response.StatusCode, responseBody);
                return BbApiResult<BbAccessTokenResponse>.Fail($"Erro ao gerar token BB: {response.StatusCode} - {responseBody}");
            }

            var result = JsonSerializer.Deserialize<BbAccessTokenResponse>(responseBody, JsonOptions);

            return result?.AccessToken != null
                ? BbApiResult<BbAccessTokenResponse>.Success(result)
                : BbApiResult<BbAccessTokenResponse>.Fail("Resposta invalida da API BB (GenerateAccessToken).");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao gerar token de acesso no BB.");
            return BbApiResult<BbAccessTokenResponse>.Fail($"Erro ao comunicar com API BB: {ex.Message}");
        }
    }

    public async Task<BbApiResult<CreateBatchPaymentResponse>> CreateBatchPaymentAsync(CreateBatchPaymentRequest request, CancellationToken cancellationToken = default)
    {
        var url = $"{_config.BaseUrl}/pagamentos-lote/boletos";

        try
        {
            var json = JsonSerializer.Serialize(request, JsonOptions);
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
            httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");
            ConfigureAuthenticatedHeaders(httpRequest);

            var response = await httpClient.SendAsync(httpRequest, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("BB CreateBatchPayment falhou: {StatusCode} - {Body}", response.StatusCode, responseBody);
                return BbApiResult<CreateBatchPaymentResponse>.Fail($"Erro ao criar lote de pagamento BB: {response.StatusCode} - {responseBody}");
            }

            var result = JsonSerializer.Deserialize<CreateBatchPaymentResponse>(responseBody, JsonOptions);

            return result != null
                ? BbApiResult<CreateBatchPaymentResponse>.Success(result)
                : BbApiResult<CreateBatchPaymentResponse>.Fail("Resposta invalida da API BB (CreateBatchPayment).");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao criar lote de pagamento no BB.");
            return BbApiResult<CreateBatchPaymentResponse>.Fail($"Erro ao comunicar com API BB: {ex.Message}");
        }
    }

    public async Task<BbApiResult<BankSlipPaymentResponse>> GetPaymentAsync(long paymentId, string agency, string account, string digit, CancellationToken cancellationToken = default)
    {
        var url = $"{_config.BaseUrl}/pagamentos-lote/boletos/{paymentId}?agencia={agency}&conta={account}&digitoConta={digit}";

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            ConfigureAuthenticatedHeaders(request);

            var response = await httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("BB GetPayment falhou para ID {PaymentId}: {StatusCode} - {Body}", paymentId, response.StatusCode, responseBody);
                return BbApiResult<BankSlipPaymentResponse>.Fail($"Erro ao consultar pagamento BB: {response.StatusCode} - {responseBody}");
            }

            var result = JsonSerializer.Deserialize<BankSlipPaymentResponse>(responseBody, JsonOptions);

            return result != null
                ? BbApiResult<BankSlipPaymentResponse>.Success(result)
                : BbApiResult<BankSlipPaymentResponse>.Fail("Resposta invalida da API BB (GetPayment).");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao consultar pagamento {PaymentId} no BB.", paymentId);
            return BbApiResult<BankSlipPaymentResponse>.Fail($"Erro ao comunicar com API BB: {ex.Message}");
        }
    }

    private void ConfigureAuthenticatedHeaders(HttpRequestMessage request)
    {
        request.Headers.TryAddWithoutValidation("gw-dev-app-key", _config.ApiKey);
    }
}
