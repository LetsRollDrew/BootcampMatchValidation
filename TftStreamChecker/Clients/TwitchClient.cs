using System.Net.Http.Json;
using TftStreamChecker.Http;
using TftStreamChecker.Logging;
using TftStreamChecker.Models;

namespace TftStreamChecker.Clients;

public class TwitchClient
{
    private readonly HttpRetryClient _http;
    private readonly ConsoleLogger _log;
    private readonly string _clientId;
    private readonly string _clientSecret;

    private string? _token;
    private DateTimeOffset _expiresAt;

    public TwitchClient(HttpRetryClient http, ConsoleLogger log, string clientId, string clientSecret)
    {
        _http = http;
        _log = log;
        _clientId = clientId;
        _clientSecret = clientSecret;
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
        return user;
    }

    public async Task<IReadOnlyList<VodInterval>> ListVodIntervals(
        string userId,
        long windowStartMs,
        long windowEndMs,
        double bufferHours,
        CancellationToken cancellationToken = default)
    {
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
        return vods;
    }

    public static int ParseDuration(string duration)
    {
        // twitch duration like 1h2m3s
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
}
