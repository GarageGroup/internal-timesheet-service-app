using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet.Service.OpenPeriod.Test;

internal sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
{
    public List<(Uri Uri, string? SubscriptionKey)> Requests { get; } = [];

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var subscriptionKey = request.Headers.TryGetValues("Ocp-Apim-Subscription-Key", out var values) ? values.Single() : null;
        Requests.Add((request.RequestUri!, subscriptionKey));

        return Task.FromResult(respond(request));
    }

    internal static HttpResponseMessage Json(string json, HttpStatusCode statusCode = HttpStatusCode.OK)
        =>
        new(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
}
