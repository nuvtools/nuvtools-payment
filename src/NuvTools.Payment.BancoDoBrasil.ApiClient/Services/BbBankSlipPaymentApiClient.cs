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
using NuvTools.Payment.BancoDoBrasil.ApiClient.Mapping;
using NuvTools.Payment.Contracts;
using NuvTools.Payment.Models.BatchPayment;
using NuvTools.Payment.Models.Common;

namespace NuvTools.Payment.BancoDoBrasil.ApiClient.Services;

/// <summary>
/// Banco do Brasil bank slip batch payment API implementation. Satisfies both the
/// provider-specific <see cref="IBbBankSlipPaymentApiClient"/> contract and the neutral
/// <see cref="IBankSlipBatchPaymentClient"/> via mapping.
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

    public async Task<IResult<CreateBatchPaymentResponse>> CreateBatchPaymentAsync(CreateBatchPaymentRequest request, CancellationToken cancellationToken = default)
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
                return Result<CreateBatchPaymentResponse>.Fail($"Erro ao criar lote de pagamento BB: {response.StatusCode} - {responseBody}", logger: logger);

            var result = JsonSerializer.Deserialize<CreateBatchPaymentResponse>(responseBody, JsonOptions);

            return result != null
                ? Result<CreateBatchPaymentResponse>.Success(result)
                : Result<CreateBatchPaymentResponse>.Fail("Resposta invalida da API BB (CreateBatchPayment).", logger: logger);
        }
        catch (Exception ex)
        {
            return Result<CreateBatchPaymentResponse>.Fail(ex, logger: logger);
        }
    }

    public async Task<IResult<BankSlipPaymentResponse>> GetPaymentAsync(long paymentId, string agency, string account, string digit, CancellationToken cancellationToken = default)
    {
        var url = $"{_config.BaseUrl}/pagamentos-lote/boletos/{paymentId}?agencia={agency}&conta={account}&digitoConta={digit}";

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            ConfigureAuthenticatedHeaders(request);

            var response = await httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                return Result<BankSlipPaymentResponse>.Fail($"Erro ao consultar pagamento BB: {response.StatusCode} - {responseBody}", logger: logger);

            var result = JsonSerializer.Deserialize<BankSlipPaymentResponse>(responseBody, JsonOptions);

            return result != null
                ? Result<BankSlipPaymentResponse>.Success(result)
                : Result<BankSlipPaymentResponse>.Fail("Resposta invalida da API BB (GetPayment).", logger: logger);
        }
        catch (Exception ex)
        {
            return Result<BankSlipPaymentResponse>.Fail(ex, logger: logger);
        }
    }

    #region IBankSlipBatchPaymentClient (neutral surface)

    async Task<IResult<AccessToken>> IBankSlipBatchPaymentClient.AuthenticateAsync(string scope, CancellationToken cancellationToken)
    {
        var result = await GenerateAccessTokenAsync(scope, cancellationToken);
        return result.Succeeded && result.Data is not null
            ? Result<AccessToken>.Success(result.Data.ToNeutral())
            : Result<AccessToken>.Fail(result.Message ?? "GenerateAccessToken failed.");
    }

    async Task<IResult<BankSlipBatchPaymentResult>> IBankSlipBatchPaymentClient.CreateBatchAsync(BankSlipBatchPaymentRequest request, CancellationToken cancellationToken)
    {
        var bbRequest = request.ToBb();
        var result = await CreateBatchPaymentAsync(bbRequest, cancellationToken);
        return result.Succeeded && result.Data is not null
            ? Result<BankSlipBatchPaymentResult>.Success(result.Data.ToNeutral())
            : Result<BankSlipBatchPaymentResult>.Fail(result.Message ?? "CreateBatchPayment failed.");
    }

    async Task<IResult<BankSlipBatchPaymentStatus>> IBankSlipBatchPaymentClient.GetBatchAsync(BankSlipBatchPaymentReference reference, CancellationToken cancellationToken)
    {
        var result = await GetPaymentAsync(reference.PaymentId, reference.Agency, reference.Account, reference.AccountDigit, cancellationToken);
        return result.Succeeded && result.Data is not null
            ? Result<BankSlipBatchPaymentStatus>.Success(result.Data.ToNeutral())
            : Result<BankSlipBatchPaymentStatus>.Fail(result.Message ?? "GetPayment failed.");
    }

    #endregion

    private void ConfigureAuthenticatedHeaders(HttpRequestMessage request)
    {
        request.Headers.TryAddWithoutValidation("gw-dev-app-key", _config.ApiKey);
    }
}
