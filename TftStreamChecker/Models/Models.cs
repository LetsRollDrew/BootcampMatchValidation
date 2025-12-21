namespace TftStreamChecker.Models;

public class RiotId
{
    public string GameName { get; init; } = string.Empty;
    public string TagLine { get; init; } = string.Empty;
    public string Routing { get; init; } = string.Empty;
}

public class MatchSummary
{
}

public class VodInterval
{
}

public class Participant
{
}

public class RiotAccountDto
{
    public string Puuid { get; set; } = string.Empty;
}

public class MatchIdList : List<string>
{
}

public class MatchDetailDto
{
    public MatchInfo? Info { get; set; }
}

public class MatchInfo
{
    public long? GameCreation { get; set; }
    public long? GameDatetime { get; set; }
    public long? GameStartTimestamp { get; set; }
    public long? GameStartTime { get; set; }
    public long? GameLength { get; set; }
    public long? GameDuration { get; set; }
    public int? QueueId { get; set; }
    public int? TftSetNumber { get; set; }
    public string? TftGameType { get; set; }
}
