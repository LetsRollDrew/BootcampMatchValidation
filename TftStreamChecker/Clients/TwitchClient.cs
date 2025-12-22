using System.Net.Http.Json;
using TftStreamChecker.Cache;
using TftStreamChecker.Http;
using TftStreamChecker.Logging;
using TftStreamChecker.Models;

namespace TftStreamChecker.Clients;

/**************************************************************************
*
* Twitch Client
*
* Grabs app tokens, users, & VOD windows with retries + cache so we
* don't brute force VODs and reuse responses across runs
**************************************************************************/
public class TwitchClient
{
    private readonly HttpRetryClient _http;
    private readonly ConsoleLogger _log;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly CacheStore? _cache;
    private readonly bool _useCache;

    private string? _token;
    private DateTimeOffset _expiresAt;

    public TwitchClient(HttpRetryClient http, ConsoleLogger log, string clientId, string clientSecret, CacheStore? cache = null, bool useCache = true)
    {
        _http = http;
        _log = log;
        _clientId = clientId;
        _clientSecret = clientSecret;
        _cache = cache;
        _useCache = useCache;
    }

    public async Task<string> GetAppToken(CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(_token) && _expiresAt > DateTimeOffset.UtcNow.AddMinutes(1))
        {
            return _token!;
        }

        var uri =
            $"https://id.twitch.tv/oauth2/token?client_id={Uri.EscapeDataString(_clientId)}&client_secret={Uri.EscapeDataString(_clientSecret)}&grant_type=client_credentials";

        _log.Verbose("requesting twitch app token");

        var response = await _http.SendAsync(() => new HttpRequestMessage(HttpMethod.Post, uri), cancellationToken);
        response.EnsureSuccessStatusCode();

        var token = await response.Content.ReadFromJsonAsync<TwitchTokenResponse>(cancellationToken: cancellationToken);
        if (token == null || string.IsNullOrWhiteSpace(token.AccessToken))
            throw new InvalidOperationException("twitch token missing from response");

        _token = token.AccessToken;
        _expiresAt = DateTimeOffset.UtcNow.AddSeconds(Math.Max(0, token.ExpiresIn));
        return _token;
    }

    public async Task<TwitchUserDto> GetUserByLogin(string login, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(login)) throw new ArgumentException("twitch login required");

        var cacheKey = CacheKeyForUser(login);
        if (_useCache && _cache != null)
        {
            var cached = _cache.Read<TwitchUserResponse>(cacheKey);
            var userCached = cached?.Data.FirstOrDefault();
            if (userCached != null && !string.IsNullOrWhiteSpace(userCached.Id))
            {
                _log.Verbose("twitch user cache hit for " + login);
                return userCached;
            }
        }

        var token = await GetAppToken(cancellationToken);
        var uri = $"https://api.twitch.tv/helix/users?login={Uri.EscapeDataString(login)}";
        _log.Verbose("fetching twitch user " + login);

        var response = await _http.SendAsync(
            () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, uri);
                request.Headers.Add("Client-ID", _clientId);
                request.Headers.Add("Authorization", "Bearer " + token);
                return request;
            },
            cancellationToken);

        response.EnsureSuccessStatusCode();
        var data = await response.Content.ReadFromJsonAsync<TwitchUserResponse>(cancellationToken: cancellationToken);
        var user = data?.Data.FirstOrDefault();
        if (user == null || string.IsNullOrWhiteSpace(user.Id))
            throw new InvalidOperationException("twitch user not found for login " + login);

        if (_useCache && _cache != null && data != null)
        {
            _cache.Write(cacheKey, data);
        }

        return user;
    }

    public async Task<IReadOnlyList<VodInterval>> ListVodIntervals(
        string userId,
        long windowStartMs,
        long windowEndMs,
        double bufferHours,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeyForVods(userId, windowStartMs, windowEndMs, bufferHours);
        if (_useCache && _cache != null)
        {
            var cached = _cache.Read<List<VodInterval>>(cacheKey);
            if (cached != null)
            {
                _log.Verbose("vod cache hit for " + userId);
                return cached;
            }
        }

        var token = await GetAppToken(cancellationToken);
        var cursor = (string?)null;
        var vods = new List<VodInterval>();
        var bufferMs = (long)(bufferHours * 3600 * 1000);

        while (true)
        {
            var uri = $"https://api.twitch.tv/helix/videos?user_id={Uri.EscapeDataString(userId)}&type=archive&first=100";
            if (!string.IsNullOrEmpty(cursor))
            {
                uri += "&after=" + Uri.EscapeDataString(cursor);
            }

            var response = await _http.SendAsync(
                () =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, uri);
                    request.Headers.Add("Client-ID", _clientId);
                    request.Headers.Add("Authorization", "Bearer " + token);
                    return request;
                },
                cancellationToken);

            response.EnsureSuccessStatusCode();
            var payload = await response.Content.ReadFromJsonAsync<TwitchVideosResponse>(cancellationToken: cancellationToken);
            if (payload == null) break;

            foreach (var vod in payload.Data)
            {
                if (string.IsNullOrWhiteSpace(vod.CreatedAt)) continue;
                var startMs = DateTimeOffset.Parse(vod.CreatedAt).ToUnixTimeMilliseconds();
                var durationSec = ParseDuration(vod.Duration);
                var endMs = startMs + durationSec * 1000;
                var intersects = endMs >= windowStartMs - bufferMs && startMs <= windowEndMs + bufferMs;
                if (intersects)
                {
                    vods.Add(new VodInterval
                    {
                        Id = vod.Id ?? string.Empty,
                        StartMs = startMs,
                        EndMs = endMs
                    });
                }
            }

            cursor = payload.Pagination?.Cursor;
            if (string.IsNullOrEmpty(cursor)) break;
        }

        vods.Sort((a, b) => a.StartMs.CompareTo(b.StartMs));

        if (_useCache && _cache != null)
        {
            _cache.Write(cacheKey, vods);
        }

        return vods;
    }

    public static int ParseDuration(string duration)
    {
        var total = 0;
        var number = 0;
        foreach (var c in duration)
        {
            if (char.IsDigit(c))
            {
                number = number * 10 + (c - '0');
                continue;
            }

            if (c == 'h')
            {
                total += number * 3600;
            }
            else if (c == 'm')
            {
                total += number * 60;
            }
            else if (c == 's')
            {
                total += number;
            }
            number = 0;
        }
        return total;
    }

    private static string CacheKeyForUser(string login)
    {
        return Path.Combine("twitchUsers", login + ".json");
    }

    private static string CacheKeyForVods(string userId, long startMs, long endMs, double bufferHours)
    {
        var file = $"{userId}_{startMs}_{endMs}_{bufferHours}.json";
        return Path.Combine("vods", file);
    }
}
