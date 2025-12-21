using TftStreamChecker.Cli;

namespace TftStreamChecker;

/*************************************************************************
*
* TFT Online/Offline Stream Checker
*
* Entry point bootstrapper
*
* adding CLI parsing and service wiring and execution here later
************************************************************************/
public static class Program
{
    public static void Main(string[] args)
    {
        // just regurgitating to check parsing
        var options = CliOptions.Parse(args);
        Console.WriteLine("riotId: " + (string.IsNullOrWhiteSpace(options.RiotId) ? "(none)" : options.RiotId));
        Console.WriteLine("twitch: " + (string.IsNullOrWhiteSpace(options.TwitchLogin) ? "(none)" : options.TwitchLogin));
        Console.WriteLine("days: " + options.Days);
        Console.WriteLine("startTime: " + (options.StartTime ?? "(none)"));
        Console.WriteLine("endTime: " + (options.EndTime ?? "(none)"));
        Console.WriteLine("eventYear: " + (options.EventYear?.ToString() ?? "(none)"));
        Console.WriteLine("eventStart: " + (options.EventStart ?? "(none)"));
        Console.WriteLine("eventEnd: " + (options.EventEnd ?? "(none)"));
        Console.WriteLine("threshold: " + options.Threshold);
    }
}
