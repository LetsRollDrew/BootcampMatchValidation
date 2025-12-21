using TftStreamChecker.Models;
using Xunit;

namespace TftStreamChecker.Tests;

public class MatchDetailDtoTests
{
    [Theory]
    [InlineData(1_700_000_000, 1_700_000_000_000)]
    [InlineData(1_700_000_000_000, 1_700_000_000_000)]
    public void normalizes_seconds_and_ms(long raw, long expected)
    {
        var info = new MatchInfo { GameCreation = raw };
        var summary = Normalize(info);
        Assert.Equal(expected, summary.startMs);
    }

    [Fact]
    public void prefers_game_creation_then_game_datetime_then_start_timestamp()
    {
        var info = new MatchInfo
        {
            GameCreation = 100,
            GameDatetime = 200,
            GameStartTimestamp = 300,
            GameStartTime = 400,
            GameLength = 10
        };
        var summary = Normalize(info);
        Assert.Equal(100_000, summary.startMs);
        Assert.Equal(110_000, summary.endMs);
    }

    [Fact]
    public void falls_back_to_start_timestamp()
    {
        var info = new MatchInfo
        {
            GameDatetime = null,
            GameCreation = null,
            GameStartTimestamp = 500,
            GameLength = 0
        };
        var summary = Normalize(info);
        Assert.Equal(500_000, summary.startMs);
    }

    private static (long startMs, long endMs) Normalize(MatchInfo info)
    {
        var startRaw = info.GameCreation
                      ?? info.GameDatetime
                      ?? info.GameStartTimestamp
                      ?? info.GameStartTime;
        if (startRaw == null) throw new InvalidOperationException("no start");
        var startMs = startRaw.Value < 1_000_000_000_000 ? startRaw.Value * 1000 : startRaw.Value;
        var duration = info.GameLength ?? info.GameDuration ?? 0;
        var endMs = startMs + duration * 1000;
        return (startMs, endMs);
    }
}
