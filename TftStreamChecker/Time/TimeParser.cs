using System.Globalization;

namespace TftStreamChecker.Time;

public static class TimeParser
{
    public static long? ParseToUnixMs(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var trimmed = value.Trim();
        if (long.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var num))
        {
            return NormalizeEpoch(num);
        }

        if (DateTimeOffset.TryParse(
                trimmed,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var dto))
        {
            return dto.ToUnixTimeMilliseconds();
        }

        throw new FormatException("could not parse time: " + value);
    }

    private static long NormalizeEpoch(long value)
    {
        // if seconds, scale to ms
        if (Math.Abs(value) < 1_000_000_000_000L) return value * 1000;
        return value;
    }
}
