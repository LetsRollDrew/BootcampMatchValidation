using TftStreamChecker.Models;

namespace TftStreamChecker.Parsing;

public static class RiotIdParser
{
    public static RiotId Parse(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) throw new ArgumentException("riotId is required");
        var parts = input.Split('#', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2) throw new ArgumentException("riotId must be in format gameName#tagLine");
        var gameName = parts[0].Trim();
        var tagLine = parts[1].Trim();
        if (gameName.Length == 0 || tagLine.Length == 0)
            throw new ArgumentException("riotId must include gameName and tagLine");
        return new RiotId
        {
            GameName = gameName,
            TagLine = tagLine,
            Routing = InferRouting(tagLine)
        };
    }

    public static string InferRouting(string tagLine)
    {
        var upper = tagLine.Trim().ToUpperInvariant();
        if (upper is "KR" or "JP") return "ASIA";
        if (upper is "EUNE" or "EUW" or "TR" or "ME1" or "RU" || upper.EndsWith("EUW2") || upper.EndsWith("EUNE"))
            return "EUROPE";
        if (upper is "OCE" or "SG2" or "TW2" or "VN2") return "SEA";
        return "AMERICAS";
    }
}
