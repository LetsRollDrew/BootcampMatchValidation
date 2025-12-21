using TftStreamChecker.Models;
using TftStreamChecker.Parsing;
using Xunit;

namespace TftStreamChecker.Tests;

public class MatchNormalizerTests
{
    [Theory]
    [InlineData(1_700_000_000, 1_700_000_000_000)]
    [InlineData(1_700_000_000_000, 1_700_000_000_000)]
    public void normalizes_seconds_and_ms(long raw, long expected)
    {
        var dto = new MatchDetailDto
        {
            Info = new MatchInfo { GameCreation = raw, GameLength = 10 }
        };
        var summary = MatchNormalizer.ToSummary("m1", dto)!;
        Assert.Equal(expected, summary.StartMs);
        Assert.Equal(expected + 10_000, summary.EndMs);
    }

    [Fact]
    public void prefers_game_creation_then_game_datetime_then_start_timestamp()
    {
        var dto = new MatchDetailDto
        {
            Info = new MatchInfo
            {
                GameCreation = 100,
                GameDatetime = 200,
                GameStartTimestamp = 300,
                GameStartTime = 400,
                GameLength = 10
            }
        };
        var summary = MatchNormalizer.ToSummary("m1", dto)!;
        Assert.Equal(100_000, summary.StartMs);
        Assert.Equal(110_000, summary.EndMs);
    }

    [Fact]
    public void falls_back_to_start_timestamp()
    {
        var dto = new MatchDetailDto
        {
            Info = new MatchInfo
            {
                GameDatetime = null,
                GameCreation = null,
                GameStartTimestamp = 500,
                GameLength = 0
            }
        };
        var summary = MatchNormalizer.ToSummary("m1", dto)!;
        Assert.Equal(500_000, summary.StartMs);
    }

    [Fact]
    public void returns_null_when_no_info()
    {
        Assert.Null(MatchNormalizer.ToSummary("m1", null));
        Assert.Null(MatchNormalizer.ToSummary("m1", new MatchDetailDto()));
    }

    [Fact]
    public void carries_queue_set_and_type()
    {
        var dto = new MatchDetailDto
        {
            Info = new MatchInfo
            {
                GameCreation = 1_700_000_000,
                GameLength = 10,
                QueueId = 1100,
                TftSetNumber = 10,
                TftGameType = "ranked"
            }
        };
        var summary = MatchNormalizer.ToSummary("m1", dto)!;
        Assert.Equal(1100, summary.QueueId);
        Assert.Equal(10, summary.SetNumber);
        Assert.Equal("ranked", summary.GameType);
    }
}
