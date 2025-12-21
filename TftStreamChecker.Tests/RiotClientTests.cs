using System.Net;
using System.Net.Http.Json;
using TftStreamChecker.Clients;
using TftStreamChecker.Http;
using TftStreamChecker.Logging;
using TftStreamChecker.Models;
using Xunit;

namespace TftStreamChecker.Tests;

public class RiotClientTests
{
    private const string Routing = "AMERICAS";

    [Fact]
    public async Task resolves_puuid()
    {
        var expected = new RiotAccountDto { Puuid = "abc123" };
        var handler = new FakeHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });

        var client = new RiotClient(
            new HttpRetryClient(new HttpClient(handler), new ConsoleLogger(false)),
            new ConsoleLogger(false),
            "key",
            cache: new Cache.CacheStore(TestTempDir));

        var puuid = await client.ResolvePuuid(new RiotId { GameName = "Tree Otter", TagLine = "3500", Routing = "AMERICAS" });

        Assert.Equal("abc123", puuid);
    }

    [Fact]
    public async Task throws_on_not_found()
    {
        var handler = new FakeHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
        var client = new RiotClient(
            new HttpRetryClient(new HttpClient(handler), new ConsoleLogger(false)),
            new ConsoleLogger(false),
            "key",
            cache: new Cache.CacheStore(TestTempDir),
            useCache: false);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.ResolvePuuid(new RiotId { GameName = "Missing", TagLine = "NA1", Routing = "AMERICAS" }));
    }

    [Fact]
    public async Task throws_on_missing_puuid()
    {
        var handler = new FakeHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new { puuid = "" })
        });
        var client = new RiotClient(
            new HttpRetryClient(new HttpClient(handler), new ConsoleLogger(false)),
            new ConsoleLogger(false),
            "key",
            cache: new Cache.CacheStore(TestTempDir),
            useCache: false);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.ResolvePuuid(new RiotId { GameName = "Tree Otter", TagLine = "3500", Routing = "AMERICAS" }));
    }

    [Fact]
    public async Task fetches_match_detail()
    {
        var dto = new MatchDetailDto { Info = new MatchInfo { GameCreation = 1_700_000_000 } };
        var handler = new FakeHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(dto)
        });

        var client = new RiotClient(
            new HttpRetryClient(new HttpClient(handler), new ConsoleLogger(false)),
            new ConsoleLogger(false),
            "key",
            cache: new Cache.CacheStore(TestTempDir),
            useCache: false);

        var result = await client.GetMatchDetail("MATCH1", Routing);

        Assert.NotNull(result);
        Assert.Equal(dto.Info!.GameCreation, result!.Info!.GameCreation);
    }

    [Fact]
    public async Task lists_match_ids_with_paging_and_max()
    {
        var responses = new[]
        {
            new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new []{ "m1", "m2" }) },
            new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new []{ "m3" }) }
        };

        var handler = new QueueHandler(responses);
        var client = new RiotClient(
            new HttpRetryClient(new HttpClient(handler), new ConsoleLogger(false)),
            new ConsoleLogger(false),
            "key",
            cache: new Cache.CacheStore(TestTempDir),
            useCache: false);

        var ids = await client.ListMatchIds("puuid", Routing, startMs: 0, endMs: 10_000, maxMatches: 3, pageSize: 2);

        Assert.Equal(new[] { "m1", "m2", "m3" }, ids);
        Assert.Equal(2, handler.Requests.Count);

        var first = handler.Requests[0].RequestUri!;
        Assert.Equal("0", QueryValue(first, "start"));
        Assert.Equal("2", QueryValue(first, "count"));
        Assert.Equal("0", QueryValue(first, "startTime"));
        Assert.Equal("10", QueryValue(first, "endTime"));

        var second = handler.Requests[1].RequestUri!;
        Assert.Equal("2", QueryValue(second, "start"));
        Assert.Equal("1", QueryValue(second, "count"));
    }

    [Fact]
    public async Task uses_cached_puuid_when_available()
    {
        var cache = new Cache.CacheStore(TestTempDir);
        cache.Write("puuid/AMERICAS_treeotter_3500.json", new RiotAccountDto { Puuid = "cached" });

        var handler = new FakeHandler(new HttpResponseMessage(HttpStatusCode.OK));
        var client = new RiotClient(
            new HttpRetryClient(new HttpClient(handler), new ConsoleLogger(false)),
            new ConsoleLogger(false),
            "key",
            cache: cache);

        var puuid = await client.ResolvePuuid(new RiotId { GameName = "treeotter", TagLine = "3500", Routing = "AMERICAS" });

        Assert.Equal("cached", puuid);
        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task uses_cached_match_list_when_available()
    {
        var cache = new Cache.CacheStore(TestTempDir);
        cache.Write("matchLists/AMERICAS_puuid_0_1000_all.json", new List<string> { "m1", "m2" });

        var handler = new QueueHandler(Array.Empty<HttpResponseMessage>());
        var client = new RiotClient(
            new HttpRetryClient(new HttpClient(handler), new ConsoleLogger(false)),
            new ConsoleLogger(false),
            "key",
            cache: cache);

        var ids = await client.ListMatchIds("puuid", Routing, 0, 1000);
        Assert.Equal(new[] { "m1", "m2" }, ids);
        Assert.Empty(handler.Requests);
    }

    private class FakeHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;
        public int RequestCount { get; private set; }

        public FakeHandler(HttpResponseMessage response)
        {
            _response = response;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            return Task.FromResult(_response);
        }
    }

    private class QueueHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses;
        public List<HttpRequestMessage> Requests { get; } = new();

        public QueueHandler(IEnumerable<HttpResponseMessage> responses)
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
    private static string TestTempDir => Path.Combine(Path.GetTempPath(), "tft-cache-tests");
}
