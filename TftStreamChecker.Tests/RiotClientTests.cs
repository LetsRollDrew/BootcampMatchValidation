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
            "key");

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
            "key");

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
            "key");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.ResolvePuuid(new RiotId { GameName = "Tree Otter", TagLine = "3500", Routing = "AMERICAS" }));
    }

    private class FakeHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;

        public FakeHandler(HttpResponseMessage response)
        {
            _response = response;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_response);
        }
    }
}
