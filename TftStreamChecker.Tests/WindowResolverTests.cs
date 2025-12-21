using TftStreamChecker.Cli;
using TftStreamChecker.Time;
using Xunit;

namespace TftStreamChecker.Tests;

public class WindowResolverTests
{
    [Fact]
    public void uses_event_defaults_when_none_provided()
    {
        var opts = new CliOptions { Days = 30 };
        var now = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var resolved = WindowResolver.Resolve(opts, now);

        var expectedStart = new DateTimeOffset(resolved.Year, 12, 3, 0, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds();
        var expectedEnd = new DateTimeOffset(resolved.Year, 12, 19, 8, 5, 0, TimeSpan.Zero).ToUnixTimeMilliseconds();

        Assert.Equal(expectedStart, resolved.EventStartMs);
        Assert.Equal(expectedEnd, resolved.EventEndMs);
        Assert.Equal(resolved.EventStartMs, resolved.StartMs);
        Assert.Equal(resolved.EventEndMs, resolved.EndMs);
    }

    [Fact]
    public void respects_explicit_times_and_year()
    {
        var opts = new CliOptions
        {
            EventYear = 2023,
            StartTime = "2023-12-04T00:00:00Z",
            EndTime = "2023-12-05T00:00:00Z",
            Days = 10
        };

        var resolved = WindowResolver.Resolve(opts);

        Assert.Equal(new DateTimeOffset(2023, 12, 4, 0, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds(), resolved.StartMs);
        Assert.Equal(new DateTimeOffset(2023, 12, 5, 0, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds(), resolved.EndMs);
        Assert.Equal(2023, resolved.Year);
    }

    [Fact]
    public void throws_when_start_after_end()
    {
        var opts = new CliOptions
        {
            StartTime = "2023-12-06T00:00:00Z",
            EndTime = "2023-12-05T00:00:00Z",
            Days = 5
        };

        Assert.Throws<InvalidOperationException>(() => WindowResolver.Resolve(opts));
    }
}
