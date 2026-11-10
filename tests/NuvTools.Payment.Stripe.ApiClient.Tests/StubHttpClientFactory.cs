namespace NuvTools.Payment.Stripe.ApiClient.Tests;

/// <summary>Hands out clients over one stub handler, for code that fetches through a factory.</summary>
internal sealed class StubHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
}
