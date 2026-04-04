using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NuvTools.Payment.Sicoob.ApiClient.Configuration;
using NuvTools.Payment.Sicoob.ApiClient.Contracts;
using NuvTools.Payment.Sicoob.ApiClient.DTOs.Requests;
using NuvTools.Payment.Sicoob.ApiClient.DTOs.Responses;

namespace NuvTools.Payment.Sicoob.ApiClient.Services;

/// <summary>
/// Implementacao do cliente da API de boletos do Sicoob.
/// </summary>
public class SicoobBankSlipApiClient(
    HttpClient httpClient,
    IOptions<SicoobApiClientConfig> options,
    ILogger<SicoobBankSlipApiClient> logger) : ISicoobBankSlipApiClient
{
    private readonly SicoobApiClientConfig _config = options.Value;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public async Task<SicoobApiResult<BankSlipResponse>> GetBankSlipAsync(int customerNumber, int modalityCode, long ourNumber, CancellationToken cancellationToken = default)
    {
        var url = $"{_config.BaseUrl}/boletos?numeroCliente={customerNumber}&codigoModalidade={modalityCode}&nossoNumero={ourNumber}";
        return await GetWithResultadoAsync<BankSlipResponse>(url, "GetBankSlip", cancellationToken);
    }

    public async Task<SicoobApiResult<List<BankSlipResponse>>> ListBankSlipsByPeriodAsync(string payerDocument, int customerNumber, DateOnly startDate, DateOnly endDate, int statusCode, CancellationToken cancellationToken = default)
    {
        var url = $"{_config.BaseUrl}/boletos?numeroCliente={customerNumber}&codigoSituacao={statusCode}" +
                  $"&dataInicio={startDate:yyyy-MM-dd}&dataFim={endDate:yyyy-MM-dd}" +
                  $"&numeroCpfCnpjPagador={payerDocument}";
        return await GetWithResultadoAsync<List<BankSlipResponse>>(url, "ListBankSlipsByPeriod", cancellationToken);
    }

    public async Task<SicoobApiResult<SecondCopyBankSlipResponse>> GetSecondCopyAsync(int customerNumber, int modalityCode, long ourNumber, CancellationToken cancellationToken = default)
    {
        var url = $"{_config.BaseUrl}/boletos/segunda-via?numeroCliente={customerNumber}&codigoModalidade={modalityCode}&nossoNumero={ourNumber}";
        return await GetWithResultadoAsync<SecondCopyBankSlipResponse>(url, "GetSecondCopy", cancellationToken);
    }

    public async Task<SicoobApiResult<CreateBankSlipResponse>> CreateBankSlipAsync(CreateBankSlipRequest request, CancellationToken cancellationToken = default)
    {
        var url = $"{_config.BaseUrl}/boletos";

        try
        {
            var json = JsonSerializer.Serialize(request, JsonOptions);
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
            httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");
            ConfigureHeaders(httpRequest);

            var response = await httpClient.SendAsync(httpRequest, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Sicoob CreateBankSlip falhou: {StatusCode} - {Body}", response.StatusCode, responseBody);
                return SicoobApiResult<CreateBankSlipResponse>.Fail($"Erro ao criar boleto Sicoob: {response.StatusCode} - {responseBody}");
            }

            var result = ExtractResultado<CreateBankSlipResponse>(responseBody);
            return result != null
                ? SicoobApiResult<CreateBankSlipResponse>.Success(result)
                : SicoobApiResult<CreateBankSlipResponse>.Fail("Resposta invalida da API Sicoob (CreateBankSlip).");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao criar boleto no Sicoob.");
            return SicoobApiResult<CreateBankSlipResponse>.Fail($"Erro ao comunicar com API Sicoob: {ex.Message}");
        }
    }

    public async Task<SicoobApiResult<bool>> CancelBankSlipAsync(long ourNumber, CancelBankSlipRequest request, CancellationToken cancellationToken = default)
    {
        var url = $"{_config.BaseUrl}/boletos/{ourNumber}/baixa";
        return await PostCommandAsync(url, request, "CancelBankSlip", cancellationToken);
    }

    public async Task<SicoobApiResult<bool>> ExtendDueDateAsync(long ourNumber, ExtendDueDateRequest request, CancellationToken cancellationToken = default)
    {
        var url = $"{_config.BaseUrl}/boletos/{ourNumber}/prorrogacao";
        return await PostCommandAsync(url, request, "ExtendDueDate", cancellationToken);
    }

    public async Task<SicoobApiResult<bool>> ChangeAmountAsync(long ourNumber, ChangeAmountRequest request, CancellationToken cancellationToken = default)
    {
        var url = $"{_config.BaseUrl}/boletos/{ourNumber}/valor";
        return await PostCommandAsync(url, request, "ChangeAmount", cancellationToken);
    }

    private void ConfigureHeaders(HttpRequestMessage request)
    {
        request.Headers.TryAddWithoutValidation("client_id", _config.ClientId);
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {_config.Token}");
    }

    private async Task<SicoobApiResult<T>> GetWithResultadoAsync<T>(string url, string operation, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            ConfigureHeaders(request);

            var response = await httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Sicoob {Operation} falhou: {StatusCode} - {Body}", operation, response.StatusCode, responseBody);
                return SicoobApiResult<T>.Fail($"Erro na API Sicoob ({operation}): {response.StatusCode} - {responseBody}");
            }

            var result = ExtractResultado<T>(responseBody);
            return result != null
                ? SicoobApiResult<T>.Success(result)
                : SicoobApiResult<T>.Fail($"Resposta invalida da API Sicoob ({operation}).");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao executar {Operation} no Sicoob.", operation);
            return SicoobApiResult<T>.Fail($"Erro ao comunicar com API Sicoob: {ex.Message}");
        }
    }

    private async Task<SicoobApiResult<bool>> PostCommandAsync(string url, object body, string operation, CancellationToken cancellationToken)
    {
        try
        {
            var json = JsonSerializer.Serialize(body, JsonOptions);
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            ConfigureHeaders(request);

            var response = await httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning("Sicoob {Operation} falhou: {StatusCode} - {Body}", operation, response.StatusCode, responseBody);
                return SicoobApiResult<bool>.Fail($"Erro na API Sicoob ({operation}): {response.StatusCode} - {responseBody}");
            }

            return SicoobApiResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao executar {Operation} no Sicoob.", operation);
            return SicoobApiResult<bool>.Fail($"Erro ao comunicar com API Sicoob: {ex.Message}");
        }
    }

    private static T? ExtractResultado<T>(string responseBody)
    {
        try
        {
            using var document = JsonDocument.Parse(responseBody);

            if (document.RootElement.TryGetProperty("resultado", out var resultado))
                return JsonSerializer.Deserialize<T>(resultado.GetRawText(), JsonOptions);

            return JsonSerializer.Deserialize<T>(responseBody, JsonOptions);
        }
        catch
        {
            return default;
        }
    }
}
