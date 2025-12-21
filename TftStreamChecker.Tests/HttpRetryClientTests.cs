using System.Net;
using System.Net.Http.Headers;
using TftStreamChecker.Http;
using TftStreamChecker.Logging;
using Xunit;

namespace TftStreamChecker.Tests;

public class HttpRetryClientTests
{
    [Fact]
    public async Task retries_on_429_with_retry_after()
    {
        var handler = new FakeHandler(
            MakeResponse(HttpStatusCode.TooManyRequests, retryAfterSeconds: 1),
            MakeResponse(HttpStatusCode.OK));
        var delays = new List<int>();
        var client = new HttpRetryClient(
            new HttpClient(handler),
            new ConsoleLogger(false),
            maxRetries: 3,
            backoffMs: 10,
            delay: (ms, _) =>
            {
                delays.Add(ms);
                return Task.CompletedTask;
            });

        var response = await client.SendAsync(() => new HttpRequestMessage(HttpMethod.Get, "https://example.com"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(delays);
        Assert.Equal(1000, delays[0]);
        Assert.Equal(2, handler.RequestCount);
    }

    [Fact]
    public async Task retries_on_503_and_uses_backoff()
    {
        var handler = new FakeHandler(
            MakeResponse(HttpStatusCode.ServiceUnavailable),
            MakeResponse(HttpStatusCode.ServiceUnavailable),
            MakeResponse(HttpStatusCode.OK));
        var delays = new List<int>();
        var client = new HttpRetryClient(
            new HttpClient(handler),
            new ConsoleLogger(false),
            maxRetries: 3,
            backoffMs: 50,
            delay: (ms, _) =>
            {
                delays.Add(ms);
                return Task.CompletedTask;
            });

        var response = await client.SendAsync(() => new HttpRequestMessage(HttpMethod.Get, "https://example.com"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, delays.Count);
        Assert.Equal(50, delays[0]);
        Assert.Equal(75, delays[1]); // 50 * 1.5
        Assert.Equal(3, handler.RequestCount);
    }

    [Fact]
    public async Task does_not_retry_on_400()
    {
        var handler = new FakeHandler(MakeResponse(HttpStatusCode.BadRequest));
        var delays = new List<int>();
        var client = new HttpRetryClient(
            new HttpClient(handler),
            new ConsoleLogger(false),
            maxRetries: 3,
            backoffMs: 10,
            delay: (ms, _) =>
            {
                delays.Add(ms);
                return Task.CompletedTask;
            });

        var response = await client.SendAsync(() => new HttpRequestMessage(HttpMethod.Get, "https://example.com"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(delays);
        Assert.Equal(1, handler.RequestCount);
    }

    private static HttpResponseMessage MakeResponse(HttpStatusCode code, int? retryAfterSeconds = null)
    {
        var msg = new HttpResponseMessage(code);
        if (retryAfterSeconds.HasValue)
        {
            msg.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(retryAfterSeconds.Value));
        }
        return msg;
    }

    private class FakeHandler : HttpMessageHandler
    {
        private readonly Queue<object> _queue;
        public int RequestCount { get; private set; }

        public FakeHandler(params object[] items)
        {
            _queue = new Queue<object>(items);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            if (_queue.Count == 0)
                throw new InvalidOperationException("no responses queued");

            var next = _queue.Dequeue();
            if (next is Exception ex)
                throw ex;

            return Task.FromResult((HttpResponseMessage)next);
        }
    }
}
