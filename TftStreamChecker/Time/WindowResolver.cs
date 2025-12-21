using TftStreamChecker.Cli;

namespace TftStreamChecker.Time;

public record ResolvedWindow(long StartMs, long EndMs, long EventStartMs, long? EventEndMs, int Year);

public static class WindowResolver
{
    public static ResolvedWindow Resolve(CliOptions options, DateTimeOffset? now = null)
    {
        var utcNow = now ?? DateTimeOffset.UtcNow;
        var year = options.EventYear ?? utcNow.Year;

        var defaultStartMs = new DateTimeOffset(year, 12, 3, 0, 0, 0, TimeSpan.Zero)
            .ToUnixTimeMilliseconds();
        var defaultEndMs = new DateTimeOffset(year, 12, 19, 8, 5, 0, TimeSpan.Zero)
            .ToUnixTimeMilliseconds();

        long eventStartMs = TimeParser.ParseToUnixMs(options.EventStart) ?? defaultStartMs;
        long? eventEndMs = TimeParser.ParseToUnixMs(options.EventEnd) ?? defaultEndMs;

        long startMs = TimeParser.ParseToUnixMs(options.StartTime) ?? eventStartMs;
        long endMs = TimeParser.ParseToUnixMs(options.EndTime)
            ?? eventEndMs
            ?? (startMs + options.Days * 86_400_000L);

        if (startMs >= endMs)
            throw new InvalidOperationException("start must be before end");

        return new ResolvedWindow(startMs, endMs, eventStartMs, eventEndMs, year);
    }

}
