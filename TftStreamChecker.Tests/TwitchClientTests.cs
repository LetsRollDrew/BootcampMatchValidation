using System.Net;
using System.Net.Http.Json;
using TftStreamChecker.Clients;
using TftStreamChecker.Http;
using TftStreamChecker.Logging;
using TftStreamChecker.Models;
using Xunit;

namespace TftStreamChecker.Tests;

public class TwitchClientTests
{
    [Fact]
    public async Task gets_and_caches_token()
    {
        var handler = new QueueHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new TwitchTokenResponse
            {
                AccessToken = "token1",
                ExpiresIn = 3600
            })
        });

        var client = new TwitchClient(
            new HttpRetryClient(new HttpClient(handler), new ConsoleLogger(false)),
            new ConsoleLogger(false),
            "id",
            "secret");

        var token1 = await client.GetAppToken();
        var token2 = await client.GetAppToken();

        Assert.Equal("token1", token1);
        Assert.Equal("token1", token2);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task refreshes_when_expired()
    {
        var handler = new QueueHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new TwitchTokenResponse
                {
                    AccessToken = "token1",
                    ExpiresIn = 1
                })
            },
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new TwitchTokenResponse
                {
                    AccessToken = "token2",
                    ExpiresIn = 3600
                })
            });

        var client = new TwitchClient(
            new HttpRetryClient(new HttpClient(handler), new ConsoleLogger(false)),
            new ConsoleLogger(false),
            "id",
            "secret");

        var token1 = await client.GetAppToken();
        await Task.Delay(1200);
        var token2 = await client.GetAppToken();

        Assert.Equal("token1", token1);
        Assert.Equal("token2", token2);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task gets_user_by_login()
    {
        var handler = new QueueHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new TwitchTokenResponse
                {
                    AccessToken = "token1",
                    ExpiresIn = 3600
                })
            },
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new TwitchUserResponse
                {
                    Data = new List<TwitchUserDto>
                    {
                        new TwitchUserDto { Id = "123", Login = "treeotter", DisplayName = "TreeOtter" }
                    }
                })
            });

        var client = new TwitchClient(
            new HttpRetryClient(new HttpClient(handler), new ConsoleLogger(false)),
            new ConsoleLogger(false),
            "id",
            "secret");

        var user = await client.GetUserByLogin("treeotter");

        Assert.Equal("123", user.Id);
        Assert.Equal(2, handler.Requests.Count);

        var req = handler.Requests.Last();
        Assert.Equal("treeotter", QueryValue(req.RequestUri!, "login"));
        Assert.Contains("Client-ID", req.Headers.Select(h => h.Key));
        Assert.Contains("Authorization", req.Headers.Select(h => h.Key));
    }

    [Fact]
    public async Task parses_durations_and_filters_vods()
    {
        var handler = new QueueHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new TwitchTokenResponse
                {
                    AccessToken = "token1",
                    ExpiresIn = 3600
                })
            },
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new TwitchVideosResponse
                {
                    Data = new List<TwitchVideoDto>
                    {
                        new TwitchVideoDto { Id = "1", CreatedAt = "2023-12-01T00:00:00Z", Duration = "1h" },
                        new TwitchVideoDto { Id = "2", CreatedAt = "2023-12-05T00:00:00Z", Duration = "30m" }
                    }
                })
            });

        var client = new TwitchClient(
            new HttpRetryClient(new HttpClient(handler), new ConsoleLogger(false)),
            new ConsoleLogger(false),
            "id",
            "secret");

        var start = new DateTimeOffset(2023, 12, 2, 0, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds();
        var end = new DateTimeOffset(2023, 12, 6, 0, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds();

        var vods = await client.ListVodIntervals("123", start, end, bufferHours: 0);

        Assert.Single(vods);
        Assert.Equal("2", vods[0].Id);
        Assert.Equal(2, handler.Requests.Count);
    }

    private class QueueHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses;
        public List<HttpRequestMessage> Requests { get; } = new();

        public QueueHandler(params HttpResponseMessage[] responses)
        {
            _responses = new Queue<HttpResponseMessage>(responses);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            if (_responses.Count == 0) throw new InvalidOperationException("no responses queued");
            return Task.FromResult(_responses.Dequeue());
        }
    }

    private static string? QueryValue(Uri uri, string key)
    {
        var query = uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in query)
        {
            var kv = part.Split('=', 2);
            if (kv.Length == 2 && kv[0] == key) return Uri.UnescapeDataString(kv[1]);
        }
        return null;
    }
}
