namespace NuvTools.Payment.BancoDoBrasil.ApiClient.Configuration;

/// <summary>
/// Configuracoes do cliente da API Banco do Brasil.
/// </summary>
public class BancoDoBrasilApiClientConfig
{
    /// <summary>
    /// Nome da secao de configuracao.
    /// </summary>
    public const string SectionName = "BancoDoBrasil";

    public required string AuthUrl { get; set; }
    public required string BaseUrl { get; set; }
    public required string ClientId { get; set; }
    public required string ClientSecret { get; set; }
    public required string ApiKey { get; set; }

    /// <summary>
    /// Escopo(s) OAuth2 (separados por espaço) solicitados no token <c>client_credentials</c>. Deve casar com
    /// o que a credencial do app está habilitada no BB. Quando vazio, usa o conjunto de guias com código de
    /// barras (<see cref="OAuth.BbScopes.GuiaPayment"/>) — o fluxo padrão deste cliente.
    /// </summary>
    public string? Scope { get; set; }
}
