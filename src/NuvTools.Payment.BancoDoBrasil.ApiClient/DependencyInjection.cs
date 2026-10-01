using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NuvTools.Payment.BancoDoBrasil.ApiClient.Configuration;
using NuvTools.Payment.BancoDoBrasil.ApiClient.Contracts;
using NuvTools.Payment.BancoDoBrasil.ApiClient.OAuth;

namespace NuvTools.Payment.BancoDoBrasil.ApiClient;

/// <summary>
/// Extensoes para registro do cliente Banco do Brasil API no container de DI.
/// </summary>
public static class DependencyInjection
{
    private const string HttpClientName = "BancoDoBrasilApi";

    /// <summary>
    /// Adiciona o cliente da API Banco do Brasil ao container de servicos.
    /// </summary>
    /// <param name="services">Colecao de servicos.</param>
    /// <param name="configuration">Configuracao da aplicacao.</param>
    /// <param name="clientCertificate">
    /// Certificado cliente para mTLS (quando a API BB exige autenticacao mutua). Quando informado, e anexado
    /// tanto ao HttpClient de pagamento quanto ao de geracao de token. O carregamento do certificado (ex.:
    /// Azure Key Vault) fica a cargo do chamador, mantendo o pacote livre de dependencias de infraestrutura.
    /// </param>
    public static IServiceCollection AddBancoDoBrasilApiClient(
        this IServiceCollection services, IConfiguration configuration, X509Certificate2? clientCertificate = null)
    {
        services.Configure<BancoDoBrasilApiClientConfig>(
            configuration.GetSection(BancoDoBrasilApiClientConfig.SectionName));

        // Provedor de token OAuth2 (cache por escopo) + handler que anexa o Bearer nas chamadas de
        // pagamento. O provedor usa um HttpClient proprio, sem o handler, para nao anexar Bearer na
        // propria geracao do token.
        services.AddSingleton<IBbTokenProvider, BbTokenProvider>();
        services.AddTransient<BbAuthHandler>();

        var tokenBuilder = services.AddHttpClient(nameof(BbTokenProvider));
        var paymentBuilder = services.AddHttpClient<IBbBankSlipPaymentApiClient, Services.BbBankSlipPaymentApiClient>(HttpClientName)
            .AddHttpMessageHandler<BbAuthHandler>();

        // mTLS: quando ha certificado cliente, anexa ao handler primario (SocketsHttpHandler) de ambos os
        // clientes. O cert so e enviado se o servidor solicitar no handshake, entao e inofensivo no endpoint
        // que nao exige mutual TLS.
        if (clientCertificate is not null)
        {
            tokenBuilder.ConfigurePrimaryHttpMessageHandler(() => CreateMtlsHandler(clientCertificate));
            paymentBuilder.ConfigurePrimaryHttpMessageHandler(() => CreateMtlsHandler(clientCertificate));
        }

        return services;
    }

    private static SocketsHttpHandler CreateMtlsHandler(X509Certificate2 certificate)
    {
        var handler = new SocketsHttpHandler();
        handler.SslOptions.ClientCertificates ??= [];
        handler.SslOptions.ClientCertificates.Add(certificate);
        handler.SslOptions.EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
        return handler;
    }
}
