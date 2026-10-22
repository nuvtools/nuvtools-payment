using System.Net;
using System.Text;

namespace NuvTools.Payment.Stripe.ApiClient.Tests;

/// <summary>
/// Stands in for api.stripe.com: answers each request from the first route whose path matches, and
/// records what was sent.
/// </summary>
internal sealed class StubStripeHandler : HttpMessageHandler
{
    private readonly List<(string PathPrefix, HttpStatusCode Status, string Body)> _routes = [];

    public List<(HttpMethod Method, string Path, string? Body)> Requests { get; } = [];

    public StubStripeHandler Answer(string pathPrefix, string body, HttpStatusCode status = HttpStatusCode.OK)
    {
        _routes.Add((pathPrefix, status, body));
        return this;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri!.AbsolutePath;
        var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);

        Requests.Add((request.Method, path, body));

        var route = _routes.FirstOrDefault(r => path.StartsWith(r.PathPrefix, StringComparison.Ordinal));

        return route.Body is null
            ? new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("""{"error":{"message":"No such route in the stub.","type":"invalid_request_error"}}""", Encoding.UTF8, "application/json")
            }
            : new HttpResponseMessage(route.Status) { Content = new StringContent(route.Body, Encoding.UTF8, "application/json") };
    }
}
