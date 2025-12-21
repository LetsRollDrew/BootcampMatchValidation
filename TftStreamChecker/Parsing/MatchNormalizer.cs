using TftStreamChecker.Models;

namespace TftStreamChecker.Parsing;

public static class MatchNormalizer
{
    public static MatchSummary? ToSummary(string matchId, MatchDetailDto? dto)
    {
        var info = dto?.Info;
        if (info == null) return null;

        var startRaw = info.GameCreation
                      ?? info.GameDatetime
                      ?? info.GameStartTimestamp
                      ?? info.GameStartTime;

        if (startRaw == null) return null;

        var startMs = startRaw.Value < 1_000_000_000_000 ? startRaw.Value * 1000 : startRaw.Value;
        var duration = info.GameLength ?? info.GameDuration ?? 0;
        var endMs = startMs + duration * 1000;

        return new MatchSummary
        {
            MatchId = matchId,
            StartMs = startMs,
            EndMs = endMs,
            QueueId = info.QueueId,
            SetNumber = info.TftSetNumber,
            GameType = info.TftGameType
        };
    }
}
