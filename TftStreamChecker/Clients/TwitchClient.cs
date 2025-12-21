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
}
