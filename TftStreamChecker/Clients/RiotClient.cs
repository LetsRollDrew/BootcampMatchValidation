using System.Net.Http.Json;
using TftStreamChecker.Http;
using TftStreamChecker.Logging;
using TftStreamChecker.Models;

namespace TftStreamChecker.Clients;

public class RiotClient
{
    private readonly HttpRetryClient _http;
    private readonly ConsoleLogger _log;
    private readonly string _apiKey;

    public RiotClient(HttpRetryClient http, ConsoleLogger log, string apiKey)
    {
        _http = http;
        _log = log;
        _apiKey = apiKey;
    }

    public async Task<string> ResolvePuuid(RiotId riotId, CancellationToken cancellationToken = default)
    {
        var uri =
            $"https://{riotId.Routing.ToLowerInvariant()}.api.riotgames.com/riot/account/v1/accounts/by-riot-id/{Uri.EscapeDataString(riotId.GameName)}/{Uri.EscapeDataString(riotId.TagLine)}";

        _log.Verbose("resolving puuid for " + riotId.GameName + "#" + riotId.TagLine + " via " + uri);

        var response = await _http.SendAsync(
            () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, uri);
                request.Headers.Add("X-Riot-Token", _apiKey);
                return request;
            },
            cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            throw new InvalidOperationException("riot id not found");

        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<RiotAccountDto>(cancellationToken: cancellationToken);
        if (dto == null || string.IsNullOrWhiteSpace(dto.Puuid))
            throw new InvalidOperationException("puuid missing from riot response");
        return dto.Puuid;
    }
}
