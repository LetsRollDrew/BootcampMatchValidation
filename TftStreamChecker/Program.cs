using TftStreamChecker.Cli;
using TftStreamChecker.Env;
using TftStreamChecker.Logging;

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
        var log = new ConsoleLogger(options.Verbose);

        EnvConfig env;
        try
        {
            env = EnvLoader.Load();
        }
        catch (Exception ex)
        {
            log.Error(ex.Message);
            return 1;
        }

        log.Info("riotId: " + (string.IsNullOrWhiteSpace(options.RiotId) ? "(none)" : options.RiotId));
        log.Info("twitch: " + (string.IsNullOrWhiteSpace(options.TwitchLogin) ? "(none)" : options.TwitchLogin));
        log.Info("days: " + options.Days);
        log.Info("startTime: " + (options.StartTime ?? "(none)"));
        log.Info("endTime: " + (options.EndTime ?? "(none)"));
        log.Info("eventYear: " + (options.EventYear?.ToString() ?? "(none)"));
        log.Info("eventStart: " + (options.EventStart ?? "(none)"));
        log.Info("eventEnd: " + (options.EventEnd ?? "(none)"));
        log.Info("threshold: " + options.Threshold);
        log.Verbose("env riot: " + Mask(env.RiotApiKey));
        log.Verbose("env twitch id: " + Mask(env.TwitchClientId));
        log.Verbose("env twitch secret: " + Mask(env.TwitchClientSecret));
        return 0;
    }

    private static string Mask(string value)
    {
        if (string.IsNullOrEmpty(value)) return "(missing)";
        if (value.Length <= 4) return new string('*', value.Length);
        return new string('*', value.Length - 4) + value[^4..];
    }
}
