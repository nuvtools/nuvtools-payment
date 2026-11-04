using System.Net;
using System.Text;

namespace NuvTools.Payment.Omie.ApiClient.Tests;

/// <summary>Answers each request with the next queued status + JSON body; the last one repeats once the queue ends.</summary>
internal sealed class SequenceHttpMessageHandler(params (HttpStatusCode Status, string Body)[] responses) : HttpMessageHandler
{
    private int _next;

    public int CallCount { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        CallCount++;
        var (status, body) = responses[Math.Min(_next++, responses.Length - 1)];

        return Task.FromResult(new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        });
    }
}
