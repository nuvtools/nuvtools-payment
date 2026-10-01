using System.Net;
using System.Net.Http.Headers;

namespace NuvTools.Payment.BancoDoBrasil.ApiClient.OAuth;

/// <summary>
/// DelegatingHandler que anexa o Bearer token OAuth2 do Banco do Brasil em cada requisicao de
/// pagamento. O <see cref="Services.BbBankSlipPaymentApiClient"/> ja adiciona a app-key
/// (<c>gw-dev-app-key</c>); este handler cuida do <c>Authorization: Bearer</c>. Em 401, invalida o
/// token e tenta uma unica vez de novo.
/// </summary>
public sealed class BbAuthHandler(IBbTokenProvider tokenProvider) : DelegatingHandler
{
    private const string Scope = BbScopes.BoletoPayment;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await ApplyTokenAsync(request, cancellationToken);

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode != HttpStatusCode.Unauthorized)
            return response;

        response.Dispose();
        tokenProvider.InvalidateToken(Scope);
        await ApplyTokenAsync(request, cancellationToken);
        return await base.SendAsync(request, cancellationToken);
    }

    private async Task ApplyTokenAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await tokenProvider.GetAccessTokenAsync(Scope, cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}
