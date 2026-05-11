using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NuvTools.Common.ResultWrapper;
using NuvTools.Payment.Sicoob.ApiClient.Configuration;
using NuvTools.Payment.Sicoob.ApiClient.Contracts;
using NuvTools.Payment.Sicoob.ApiClient.DTOs.Requests;
using NuvTools.Payment.Sicoob.ApiClient.DTOs.Responses;

namespace NuvTools.Payment.Sicoob.ApiClient.Services;

/// <summary>
/// Sicoob bank slip API implementation.
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

    public async Task<IResult<BankSlipResponse>> GetBankSlipAsync(int customerNumber, int modalityCode, long ourNumber, CancellationToken cancellationToken = default)
    {
        var url = $"{_config.BaseUrl}/boletos?numeroCliente={customerNumber}&codigoModalidade={modalityCode}&nossoNumero={ourNumber}";
        return await GetWithResultadoAsync<BankSlipResponse>(url, "GetBankSlip", cancellationToken);
    }

    public async Task<IResult<List<BankSlipResponse>>> ListBankSlipsByPeriodAsync(string payerDocument, int customerNumber, DateOnly startDate, DateOnly endDate, int statusCode, CancellationToken cancellationToken = default)
    {
        var url = $"{_config.BaseUrl}/boletos?numeroCliente={customerNumber}&codigoSituacao={statusCode}" +
                  $"&dataInicio={startDate:yyyy-MM-dd}&dataFim={endDate:yyyy-MM-dd}" +
                  $"&numeroCpfCnpjPagador={payerDocument}";
        return await GetWithResultadoAsync<List<BankSlipResponse>>(url, "ListBankSlipsByPeriod", cancellationToken);
    }

    public async Task<IResult<SecondCopyBankSlipResponse>> GetSecondCopyAsync(int customerNumber, int modalityCode, long ourNumber, CancellationToken cancellationToken = default)
    {
        var url = $"{_config.BaseUrl}/boletos/segunda-via?numeroCliente={customerNumber}&codigoModalidade={modalityCode}&nossoNumero={ourNumber}";
        return await GetWithResultadoAsync<SecondCopyBankSlipResponse>(url, "GetSecondCopy", cancellationToken);
    }

    public async Task<IResult<CreateBankSlipResponse>> CreateBankSlipAsync(CreateBankSlipRequest request, CancellationToken cancellationToken = default)
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
                return Result<CreateBankSlipResponse>.Fail($"Erro ao criar boleto Sicoob: {response.StatusCode} - {responseBody}", logger: logger);

            var result = ExtractResultado<CreateBankSlipResponse>(responseBody);
            return result != null
                ? Result<CreateBankSlipResponse>.Success(result)
                : Result<CreateBankSlipResponse>.Fail("Resposta invalida da API Sicoob (CreateBankSlip).", logger: logger);
        }
        catch (Exception ex)
        {
            return Result<CreateBankSlipResponse>.Fail(ex, logger: logger);
        }
    }

    public async Task<IResult> CancelBankSlipAsync(long ourNumber, CancelBankSlipRequest request, CancellationToken cancellationToken = default)
    {
        var url = $"{_config.BaseUrl}/boletos/{ourNumber}/baixa";
        return await PostCommandAsync(url, request, "CancelBankSlip", cancellationToken);
    }

    public async Task<IResult> ExtendDueDateAsync(long ourNumber, ExtendDueDateRequest request, CancellationToken cancellationToken = default)
    {
        var url = $"{_config.BaseUrl}/boletos/{ourNumber}/prorrogacao";
        return await PostCommandAsync(url, request, "ExtendDueDate", cancellationToken);
    }

    public async Task<IResult> ChangeAmountAsync(long ourNumber, ChangeAmountRequest request, CancellationToken cancellationToken = default)
    {
        var url = $"{_config.BaseUrl}/boletos/{ourNumber}/valor";
        return await PostCommandAsync(url, request, "ChangeAmount", cancellationToken);
    }

    private void ConfigureHeaders(HttpRequestMessage request)
    {
        request.Headers.TryAddWithoutValidation("client_id", _config.ClientId);
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {_config.Token}");
    }

    private async Task<IResult<T>> GetWithResultadoAsync<T>(string url, string operation, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            ConfigureHeaders(request);

            var response = await httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                return Result<T>.Fail($"Erro na API Sicoob ({operation}): {response.StatusCode} - {responseBody}", logger: logger);

            var result = ExtractResultado<T>(responseBody);
            return result != null
                ? Result<T>.Success(result)
                : Result<T>.Fail($"Resposta invalida da API Sicoob ({operation}).", logger: logger);
        }
        catch (Exception ex)
        {
            return Result<T>.Fail(ex, logger: logger);
        }
    }

    private async Task<IResult> PostCommandAsync(string url, object body, string operation, CancellationToken cancellationToken)
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
                return Result.Fail($"Erro na API Sicoob ({operation}): {response.StatusCode} - {responseBody}", logger: logger);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Fail(ex, logger: logger);
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
