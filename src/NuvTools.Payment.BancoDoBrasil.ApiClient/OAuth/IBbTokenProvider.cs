namespace NuvTools.Payment.BancoDoBrasil.ApiClient.OAuth;

/// <summary>
/// Provedor de tokens de acesso OAuth2 (client_credentials) da API Banco do Brasil.
/// Registrado como Singleton para manter o cache de tokens entre requisicoes.
/// </summary>
public interface IBbTokenProvider
{
    /// <summary>
    /// Obtem um token de acesso valido para o escopo informado, reaproveitando o cache quando possivel.
    /// </summary>
    Task<string> GetAccessTokenAsync(string scope, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalida o token em cache do escopo informado, forcando nova geracao na proxima chamada.
    /// </summary>
    void InvalidateToken(string scope);
}
