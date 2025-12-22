namespace TftStreamChecker.Models;

public class RiotId
{
    public string GameName { get; init; } = string.Empty;
    public string TagLine { get; init; } = string.Empty;
    public string Routing { get; init; } = string.Empty;
}

public class MatchSummary
{
    public string MatchId { get; set; } = string.Empty;
    public long StartMs { get; set; }
    public long EndMs { get; set; }
    public int? QueueId { get; set; }
    public int? SetNumber { get; set; }
    public string? GameType { get; set; }
}

public class VodInterval
{
    public string Id { get; set; } = string.Empty;
    public long StartMs { get; set; }
    public long EndMs { get; set; }
}

public class Participant
{
    public string? Name { get; set; }
    public double? Rank { get; set; }
    public List<Social>? Socials { get; set; }
    public string? RankUrl { get; set; }
    public bool? Eliminated { get; set; }
    public int? DayEliminated { get; set; }
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

public class TwitchTokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
}

public class TwitchUserResponse
{
    public List<TwitchUserDto> Data { get; set; } = new();
}

public class TwitchUserDto
{
    public string Id { get; set; } = string.Empty;
    public string Login { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}

public class TwitchVideosResponse
{
    public List<TwitchVideoDto> Data { get; set; } = new();
    public TwitchPaginationDto? Pagination { get; set; }
}

public class TwitchVideoDto
{
    public string? Id { get; set; }
    public string Duration { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
}

public class TwitchPaginationDto
{
    public string? Cursor { get; set; }
}

public class Social
{
    public string? LinkUri { get; set; }
}

public class MatchStats
{
    public int Total { get; set; }
    public int OnStream { get; set; }
    public int OffStream { get; set; }
    public int Unknown { get; set; }
    public double PctKnown { get; set; }
    public double PctTotal { get; set; }
    public bool Pass { get; set; }
}
