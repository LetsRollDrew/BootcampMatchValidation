namespace TftStreamChecker.Cli;

public class CliOptions
{
    public string RiotId { get; init; } = string.Empty;
    public string TwitchLogin { get; init; } = string.Empty;
    public int Days { get; init; } = 30;
    public string? StartTime { get; init; }
    public string? EndTime { get; init; }
    public int? EventYear { get; init; }
    public string? EventStart { get; init; }
    public string? EventEnd { get; init; }
    public double Threshold { get; init; } = 0.5;
    public bool Verbose { get; init; }
    public string? OutputCsv { get; init; } = "output/stream-check.csv";
    public bool UseCache { get; init; } = true;
    public string? InputPath { get; init; }
    public int Concurrency { get; init; } = 1;

    public static CliOptions Parse(string[] args)
    {
        var riotId = string.Empty;
        var twitch = string.Empty;
        var days = 30;
        string? start = null;
        string? end = null;
        int? eventYear = null;
        string? eventStart = null;
        string? eventEnd = null;
        double threshold = 0.5;
        var verbose = false;
        string? outputCsv = "output/stream-check.csv";
        var useCache = true;
        string? inputPath = null;
        var concurrency = 1;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            string? Next() => i + 1 < args.Length ? args[i + 1] : null;

            switch (arg)
            {
                case "--riotId":
                    riotId = Next() ?? string.Empty;
                    i++;
                    break;
                case "--twitch":
                    twitch = Next() ?? string.Empty;
                    i++;
                    break;
                case "--days":
                    if (int.TryParse(Next(), out var parsedDays))
                    {
                        days = parsedDays;
                    }
                    i++;
                    break;
                case "--startTime":
                    start = Next();
                    i++;
                    break;
                case "--endTime":
                    end = Next();
                    i++;
                    break;
                case "--eventYear":
                    if (int.TryParse(Next(), out var parsedYear))
                    {
                        eventYear = parsedYear;
                    }
                    i++;
                    break;
                case "--eventStart":
                    eventStart = Next();
                    i++;
                    break;
                case "--eventEnd":
                    eventEnd = Next();
                    i++;
                    break;
                case "--threshold":
                    if (double.TryParse(Next(), out var parsedThreshold))
                    {
                        threshold = parsedThreshold;
                    }
                    i++;
                    break;
                case "--verbose":
                    verbose = true;
                    break;
                case "--outputCsv":
                    outputCsv = Next();
                    i++;
                    break;
                case "--noCache":
                    useCache = false;
                    break;
                case "--input":
                    inputPath = Next();
                    i++;
                    break;
                case "--concurrency":
                    if (int.TryParse(Next(), out var parsedConc))
                    {
                        concurrency = parsedConc;
                    }
                    i++;
                    break;
            }
        }

        return new CliOptions
        {
            RiotId = riotId,
            TwitchLogin = twitch,
            Days = days,
            StartTime = start,
            EndTime = end,
            EventYear = eventYear,
            EventStart = eventStart,
            EventEnd = eventEnd,
            Threshold = threshold,
            Verbose = verbose,
            OutputCsv = outputCsv,
            UseCache = useCache,
            InputPath = inputPath,
            Concurrency = concurrency
        };
    }
}
