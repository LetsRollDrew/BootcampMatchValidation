using System.Globalization;
using TftStreamChecker.Models;

namespace TftStreamChecker.Output;

public static class SummaryPrinter
{
    public static void Print(string name, RiotId riotId, string twitch, MatchStats stats, double threshold)
    {
        Console.WriteLine($"=== {name} ({riotId.GameName}#{riotId.TagLine}) twitch:{twitch} ===");
        Console.WriteLine($"Total matches: {stats.Total}");
        Console.WriteLine($"On-stream: {stats.OnStream}");
        Console.WriteLine($"Off-stream: {stats.OffStream}");
        Console.WriteLine($"Unknown: {stats.Unknown}");
        Console.WriteLine($"On-stream % (known-only): {FormatPercent(stats.PctKnown)}");
        Console.WriteLine($"On-stream % (total): {FormatPercent(stats.PctTotal)}");
        Console.WriteLine($"Result: {(stats.Pass ? "PASS" : "FAIL")} (threshold {(threshold * 100):0}%)");
        Console.WriteLine("");
    }

    private static string FormatPercent(double value) => (value * 100).ToString("0.00", CultureInfo.InvariantCulture) + "%";
}
