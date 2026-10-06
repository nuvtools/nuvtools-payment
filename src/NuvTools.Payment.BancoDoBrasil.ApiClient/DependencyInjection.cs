using System.Net.Security;
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
    /// <param name="trustedServerRootCertificates">
    /// Raizes adicionais em que confiar ao validar o certificado do SERVIDOR do BB — ex.: a CA interna de
    /// homologacao ("AC Banco do Brasil v3 HOM"), que nao esta nos repositorios publicos e faz o handshake
    /// falhar com <c>UntrustedRoot</c>. Complementa a validacao padrao (nao a substitui): certificado valido
    /// pelas raizes do sistema segue aceito; so o erro de cadeia e reavaliado contra estas raizes; erro de nome
    /// do host continua recusado. Nulo/vazio = validacao padrao apenas.
    /// </param>
    public static IServiceCollection AddBancoDoBrasilApiClient(
        this IServiceCollection services, IConfiguration configuration, X509Certificate2? clientCertificate = null,
        IReadOnlyCollection<X509Certificate2>? trustedServerRootCertificates = null)
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
        // que nao exige mutual TLS. Raizes adicionais do servidor entram no mesmo handler.
        var trustedRoots = trustedServerRootCertificates is { Count: > 0 } ? trustedServerRootCertificates : null;
        if (clientCertificate is not null || trustedRoots is not null)
        {
            tokenBuilder.ConfigurePrimaryHttpMessageHandler(() => CreateHandler(clientCertificate, trustedRoots));
            paymentBuilder.ConfigurePrimaryHttpMessageHandler(() => CreateHandler(clientCertificate, trustedRoots));
        }

        return services;
    }

    private static SocketsHttpHandler CreateHandler(
        X509Certificate2? clientCertificate, IReadOnlyCollection<X509Certificate2>? trustedRoots)
    {
        var handler = new SocketsHttpHandler();
        handler.SslOptions.EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;

        if (clientCertificate is not null)
        {
            handler.SslOptions.ClientCertificates ??= [];
            handler.SslOptions.ClientCertificates.Add(clientCertificate);
        }

        if (trustedRoots is not null)
            handler.SslOptions.RemoteCertificateValidationCallback =
                (_, certificate, chain, errors) => ValidateServerCertificate(certificate, chain, errors, trustedRoots);

        return handler;
    }

    /// <summary>
    /// Aceita o certificado do servidor quando a validacao padrao passa; quando o UNICO problema e a cadeia
    /// (ex.: <c>UntrustedRoot</c> da CA de homologacao do BB), reconstroi a cadeia confiando apenas nas raizes
    /// informadas. Qualquer outro erro (nome do host divergente, certificado ausente) e recusado.
    /// </summary>
    internal static bool ValidateServerCertificate(
        X509Certificate? certificate, X509Chain? presentedChain, SslPolicyErrors errors,
        IReadOnlyCollection<X509Certificate2> trustedRoots)
    {
        if (errors == SslPolicyErrors.None)
            return true;

        if (errors != SslPolicyErrors.RemoteCertificateChainErrors || certificate is null)
            return false;

        using var chain = new X509Chain();
        chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
        // Mesmo comportamento padrao do SslStream cliente: sem consulta de revogacao (a LCR da CA interna do BB
        // nao e alcancavel de fora da rede do banco).
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        foreach (var root in trustedRoots)
            chain.ChainPolicy.CustomTrustStore.Add(root);

        // Intermediarias enviadas pelo servidor no handshake.
        if (presentedChain is not null)
            foreach (var element in presentedChain.ChainElements)
                chain.ChainPolicy.ExtraStore.Add(element.Certificate);

        var leaf = certificate as X509Certificate2 ?? new X509Certificate2(certificate);
        return chain.Build(leaf);
    }
}
