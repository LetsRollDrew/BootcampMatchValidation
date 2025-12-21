using System.Net;
using System.Net.Http.Headers;
using TftStreamChecker.Logging;

namespace TftStreamChecker.Http;

public class HttpRetryClient
{
    private readonly HttpClient _client;
    private readonly ConsoleLogger _log;
    private readonly int _maxRetries;
    private readonly int _backoffMs;
    private readonly Func<int, CancellationToken, Task> _delay;

    public HttpRetryClient(
        HttpClient client,
        ConsoleLogger log,
        int maxRetries = 5,
        int backoffMs = 1000,
        Func<int, CancellationToken, Task>? delay = null)
    {
        _client = client;
        _log = log;
        _maxRetries = Math.Max(1, maxRetries);
        _backoffMs = Math.Max(1, backoffMs);
        _delay = delay ?? ((ms, ct) => Task.Delay(ms, ct));
    }

    public async Task<HttpResponseMessage> SendAsync(
        Func<HttpRequestMessage> requestFactory,
        CancellationToken cancellationToken = default)
    {
        var delayMs = _backoffMs;
        for (var attempt = 1; attempt <= _maxRetries; attempt++)
        {
            using var request = requestFactory();
            HttpResponseMessage response;
            try
            {
                response = await _client.SendAsync(request, cancellationToken);
            }
            catch (HttpRequestException ex) when (attempt < _maxRetries)
            {
                _log.Verbose($"retry {attempt}/{_maxRetries} after exception: {ex.Message}");
                await _delay(delayMs, cancellationToken);
                delayMs = NextDelay(delayMs);
                continue;
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested && attempt < _maxRetries)
            {
                _log.Verbose($"retry {attempt}/{_maxRetries} after timeout: {ex.Message}");
                await _delay(delayMs, cancellationToken);
                delayMs = NextDelay(delayMs);
                continue;
            }

            if (ShouldRetry(response.StatusCode) && attempt < _maxRetries)
            {
                var waitMs = GetRetryAfterMs(response.Headers) ?? delayMs;
                _log.Verbose($"retry {attempt}/{_maxRetries} after {waitMs}ms (status {(int)response.StatusCode})");
                await _delay(waitMs, cancellationToken);
                delayMs = NextDelay(delayMs);
                continue;
            }

            return response;
        }

        throw new InvalidOperationException("unreachable retry state");
    }

    private static bool ShouldRetry(HttpStatusCode code)
    {
        var numeric = (int)code;
        if (numeric == 429) return true;
        return numeric >= 500 && numeric < 600;
    }

    private static int? GetRetryAfterMs(HttpResponseHeaders headers)
    {
        if (headers.RetryAfter == null) return null;
        if (headers.RetryAfter.Delta.HasValue)
        {
            return (int)Math.Max(0, headers.RetryAfter.Delta.Value.TotalMilliseconds);
        }
        if (headers.RetryAfter.Date.HasValue)
        {
            var ms = (int)(headers.RetryAfter.Date.Value - DateTimeOffset.UtcNow).TotalMilliseconds;
            return ms > 0 ? ms : null;
        }
        return null;
    }

    private static int NextDelay(int current) => (int)Math.Ceiling(current * 1.5);
}
