using System.Net.Http.Json;
using TftStreamChecker.Cache;
using TftStreamChecker.Http;
using TftStreamChecker.Logging;
using TftStreamChecker.Models;

namespace TftStreamChecker.Clients;

public class RiotClient
{
    private readonly HttpRetryClient _http;
    private readonly ConsoleLogger _log;
    private readonly string _apiKey;
    private readonly CacheStore? _cache;
    private readonly bool _useCache;

    public RiotClient(HttpRetryClient http, ConsoleLogger log, string apiKey, CacheStore? cache = null, bool useCache = true)
    {
        _http = http;
        _log = log;
        _apiKey = apiKey;
        _cache = cache;
        _useCache = useCache;
    }

    public async Task<string> ResolvePuuid(RiotId riotId, CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeyForPuuid(riotId);
        if (_useCache && _cache != null)
        {
            var cached = _cache.Read<RiotAccountDto>(cacheKey);
            if (cached?.Puuid?.Length > 0)
            {
                _log.Verbose("puuid cache hit for " + riotId.GameName + "#" + riotId.TagLine);
                return cached.Puuid;
            }
        }

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

        if (_useCache && _cache != null)
        {
            _cache.Write(cacheKey, dto);
        }

        return dto.Puuid;
    }

    public async Task<IReadOnlyList<string>> ListMatchIds(
        string puuid,
        string routing,
        long startMs,
        long endMs,
        int? maxMatches = null,
        int pageSize = 200,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeyForMatchList(puuid, routing, startMs, endMs, maxMatches);
        if (_useCache && _cache != null)
        {
            var cached = _cache.Read<List<string>>(cacheKey);
            if (cached != null && cached.Count > 0)
            {
                _log.Verbose("match list cache hit for " + puuid);
                if (maxMatches.HasValue && cached.Count > maxMatches.Value)
                {
                    return cached.Take(maxMatches.Value).ToList();
                }
                return cached;
            }
        }

        var ids = new List<string>();
        var start = 0;
        var clampedPage = Math.Max(1, pageSize);
        var startSec = startMs / 1000;
        var endSec = endMs / 1000;

        while (true)
        {
            if (maxMatches.HasValue && ids.Count >= maxMatches.Value) break;
            var remaining = maxMatches.HasValue ? Math.Min(clampedPage, maxMatches.Value - ids.Count) : clampedPage;
            var uri = BuildIdsUri(puuid, routing, startSec, endSec, start, remaining);

            var response = await _http.SendAsync(
                () =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, uri);
                    request.Headers.Add("X-Riot-Token", _apiKey);
                    return request;
                },
                cancellationToken);

            response.EnsureSuccessStatusCode();
            var batch = await response.Content.ReadFromJsonAsync<List<string>>(cancellationToken: cancellationToken) ?? new List<string>();
            ids.AddRange(batch);
            if (batch.Count < remaining) break;
            start += remaining;
        }

        if (maxMatches.HasValue && ids.Count > maxMatches.Value)
        {
            ids = ids.Take(maxMatches.Value).ToList();
        }

        if (_useCache && _cache != null)
        {
            _cache.Write(cacheKey, ids);
        }

        return ids;
    }

    private static string BuildIdsUri(string puuid, string routing, long startSec, long endSec, int start, int count)
    {
        var url = $"https://{routing.ToLowerInvariant()}.api.riotgames.com/tft/match/v1/matches/by-puuid/{Uri.EscapeDataString(puuid)}/ids";
        var query = $"?startTime={startSec}&endTime={endSec}&start={start}&count={count}";
        return url + query;
    }

    public async Task<MatchDetailDto?> GetMatchDetail(string matchId, string routing, CancellationToken cancellationToken = default)
    {
        var uri = $"https://{routing.ToLowerInvariant()}.api.riotgames.com/tft/match/v1/matches/{Uri.EscapeDataString(matchId)}";

        var response = await _http.SendAsync(
            () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, uri);
                request.Headers.Add("X-Riot-Token", _apiKey);
                return request;
            },
            cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _log.Verbose("match not found: " + matchId);
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MatchDetailDto>(cancellationToken: cancellationToken);
    }

    private static string CacheKeyForPuuid(RiotId riotId)
    {
        var file = $"{riotId.Routing}_{Uri.EscapeDataString(riotId.GameName)}_{Uri.EscapeDataString(riotId.TagLine)}.json";
        return Path.Combine("puuid", file);
    }

    private static string CacheKeyForMatchList(string puuid, string routing, long startMs, long endMs, int? maxMatches)
    {
        var file = $"{routing}_{puuid}_{startMs}_{endMs}_{maxMatches?.ToString() ?? "all"}.json";
        return Path.Combine("matchLists", file);
    }
}
