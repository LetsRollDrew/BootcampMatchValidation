using System.Globalization;
using System.Text;
using TftStreamChecker.Models;

namespace TftStreamChecker.Output;

public static class CsvWriter
{
    private static readonly string[] Header =
    {
        "Name",
        "GameName",
        "TagLine",
        "Twitch",
        "Total",
        "OnStream",
        "OffStream",
        "Unknown",
        "PctTotal",
        "Result"
    };

    public static void Append(string path, string name, RiotId riotId, string twitch, MatchStats stats, string? resultOverride = null)
    {
        if (string.IsNullOrWhiteSpace(path)) return;

        var fullPath = Path.GetFullPath(path);
        var dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var needsHeader = !File.Exists(fullPath);
        var line = string.Join(",",
            Escape(name),
            Escape(riotId.GameName),
            Escape(riotId.TagLine),
            Escape(twitch),
            stats.Total,
            stats.OnStream,
            stats.OffStream,
            stats.Unknown,
            Escape(FormatPercent(stats.PctTotal)),
            Escape(resultOverride ?? (stats.Pass ? "PASS" : "FAIL")));

        using var writer = new StreamWriter(fullPath, append: true, Encoding.UTF8);
        if (needsHeader)
        {
            writer.WriteLine(string.Join(",", Header));
        }
        writer.WriteLine(line);
    }

    private static string Escape(object value)
    {
        var text = value?.ToString() ?? string.Empty;
        if (text.Contains(',') || text.Contains('"'))
        {
            text = "\"" + text.Replace("\"", "\"\"") + "\"";
        }
        return text;
    }

    private static string FormatPercent(double value) => (value * 100).ToString("0.00", CultureInfo.InvariantCulture) + "%";
}
