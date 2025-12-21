using TftStreamChecker.Cli;
using TftStreamChecker.Env;

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
    public static int Main(string[] args)
    {
        var options = CliOptions.Parse(args);
        EnvConfig env;

        try
        {
            env = EnvLoader.Load();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }

        Console.WriteLine("riotId: " + (string.IsNullOrWhiteSpace(options.RiotId) ? "(none)" : options.RiotId));
        Console.WriteLine("twitch: " + (string.IsNullOrWhiteSpace(options.TwitchLogin) ? "(none)" : options.TwitchLogin));
        Console.WriteLine("days: " + options.Days);
        Console.WriteLine("startTime: " + (options.StartTime ?? "(none)"));
        Console.WriteLine("endTime: " + (options.EndTime ?? "(none)"));
        Console.WriteLine("eventYear: " + (options.EventYear?.ToString() ?? "(none)"));
        Console.WriteLine("eventStart: " + (options.EventStart ?? "(none)"));
        Console.WriteLine("eventEnd: " + (options.EventEnd ?? "(none)"));
        Console.WriteLine("threshold: " + options.Threshold);
        return 0;
    }

    private static string Mask(string value)
    {
        if (string.IsNullOrEmpty(value)) return "(missing)";
        if (value.Length <= 4) return new string('*', value.Length);
        return new string('*', value.Length - 4) + value[^4..];
    }
}
