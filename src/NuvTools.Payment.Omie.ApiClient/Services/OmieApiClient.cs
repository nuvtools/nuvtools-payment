using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NuvTools.Payment.Omie.ApiClient.Configuration;
using NuvTools.Payment.Omie.ApiClient.Contracts;
using NuvTools.Payment.Omie.ApiClient.DTOs.Requests;
using NuvTools.Payment.Omie.ApiClient.DTOs.Responses;
using NuvTools.Payment.Omie.ApiClient.Resources;

namespace NuvTools.Payment.Omie.ApiClient.Services;

/// <summary>
/// Implementacao do cliente da API Omie.
/// </summary>
public class OmieApiClient(
    HttpClient httpClient,
    IOptions<OmieApiClientConfig> options,
    ILogger<OmieApiClient> logger) : IOmieApiClient
{
    private readonly OmieApiClientConfig _config = options.Value;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public async Task<OmieApiResult<bool>> ConsultarClienteAsync(long codigoClienteOmie, CancellationToken cancellationToken = default)
    {
        var request = BuildRequest(Fields.ConsultClient, new[] { new { codigo_cliente_omie = codigoClienteOmie } });

        var response = await SendAsync(request, _config.BaseUrlClient, cancellationToken);

        if (response == null)
            return OmieApiResult<bool>.Fail(string.Format(Messages.FailedCommunicationX, Messages.WhenConsultingOmieClient));

        if (response.IsSuccessStatusCode)
            return OmieApiResult<bool>.Success(true);

        var errorMessage = await ParseErrorAsync(response, cancellationToken);
        logger.LogWarning("Omie ConsultarCliente falhou para codigo {CodigoClienteOmie}: {Error}", codigoClienteOmie, errorMessage);
        return OmieApiResult<bool>.Fail(errorMessage);
    }

    public async Task<OmieApiResult<ConsultarCadastroServicoResponse>> ConsultarCadastroServicoAsync(long serviceCodeOmie, CancellationToken cancellationToken = default)
    {
        var request = BuildRequest(Fields.ConsultServiceRegistration, new[] { new { nCodServ = serviceCodeOmie } });

        var response = await SendAsync(request, _config.BaseUrlService, cancellationToken);

        if (response == null)
            return OmieApiResult<ConsultarCadastroServicoResponse>.Fail(string.Format(Messages.FailedCommunicationX, Messages.WhenConsultingOmieService));

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = ParseError(responseBody);
            logger.LogWarning("Omie ConsultarCadastroServico falhou para servico {ServiceCodeOmie}: {Error}", serviceCodeOmie, errorMessage);
            return OmieApiResult<ConsultarCadastroServicoResponse>.Fail(errorMessage);
        }

        var result = JsonSerializer.Deserialize<ConsultarCadastroServicoResponse>(responseBody, JsonOptions);

        if (result == null)
            return OmieApiResult<ConsultarCadastroServicoResponse>.Fail("Resposta invalida da API Omie (ConsultarCadastroServico).");

        return OmieApiResult<ConsultarCadastroServicoResponse>.Success(result);
    }

    public async Task<OmieApiResult<IncluirOSResponse>> IncluirOSAsync(IncluirOSParam param, CancellationToken cancellationToken = default)
    {
        var request = BuildRequest(Fields.IncludeOS, new[] { param });

        var response = await SendAsync(request, _config.BaseUrlOrderService, cancellationToken);

        if (response == null)
            return OmieApiResult<IncluirOSResponse>.Fail(string.Format(Messages.FailedCommunicationX, Messages.WhenIncludingOmieWorkOrder));

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = ParseError(responseBody);
            logger.LogWarning("Omie IncluirOS falhou: {Error}", errorMessage);
            return OmieApiResult<IncluirOSResponse>.Fail(errorMessage);
        }

        var result = JsonSerializer.Deserialize<IncluirOSResponse>(responseBody, JsonOptions);

        if (result == null)
            return OmieApiResult<IncluirOSResponse>.Fail("Resposta invalida da API Omie (IncluirOS).");
        //return OmieApiResult<IncluirOSResponse>.Fail("Resposta invalida da API Omie (IncluirOS).");

        return OmieApiResult<IncluirOSResponse>.Success(result);
    }

    public async Task<OmieApiResult<FaturarOSResponse>> FaturarOSAsync(string internalCodeOS, CancellationToken cancellationToken = default)
    {
        var request = BuildRequest(Fields.InvoiceOS, new[] { new { cCodIntOS = internalCodeOS } });

        var response = await SendAsync(request, _config.BaseUrlOrderBilling, cancellationToken);

        if (response == null)
            return OmieApiResult<FaturarOSResponse>.Fail(string.Format(Messages.FailedCommunicationX, Messages.WhenInvoicingOmieServiceOrder));
        
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = ParseError(responseBody);
            logger.LogWarning("Omie FaturarOS falhou para OS {InternalCodeOS}: {Error}", internalCodeOS, errorMessage);
            return OmieApiResult<FaturarOSResponse>.Fail(errorMessage);
        }

        var result = JsonSerializer.Deserialize<FaturarOSResponse>(responseBody, JsonOptions);

        if (result == null)
            return OmieApiResult<FaturarOSResponse>.Fail("Resposta invalida da API Omie (FaturarOS).");

        return OmieApiResult<FaturarOSResponse>.Success(result);
    }

    private object BuildRequest<T>(string call, T param)
    {
        return new
        {
            call,
            app_key = _config.AppKey,
            app_secret = _config.AppSecret,
            param
        };
    }

    private async Task<HttpResponseMessage?> SendAsync(object requestBody, string url, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(requestBody, JsonOptions);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            return await httpClient.SendAsync(httpRequest, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao enviar requisicao para API Omie: {Url}", url);
            return null;
        }
    }

    private static string ParseError(string responseBody)
    {
        try
        {
            var error = JsonSerializer.Deserialize<OmieErrorResponse>(responseBody, JsonOptions);
            if (!string.IsNullOrEmpty(error?.FaultString))
                return error.FaultString;
        }
        catch
        {
            // Ignora erro de desserializacao
        }

        return !string.IsNullOrEmpty(responseBody)
            ? responseBody
            : "Erro desconhecido na API Omie.";
    }

    private static async Task<string> ParseErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        return ParseError(responseBody);
    }
}
