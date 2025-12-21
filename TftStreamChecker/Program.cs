using TftStreamChecker.Cli;
using TftStreamChecker.Env;
using TftStreamChecker.Logging;
using TftStreamChecker.Parsing;
using TftStreamChecker.Time;
using TftStreamChecker.Http;
using TftStreamChecker.Clients;
using TftStreamChecker.Models;
using TftStreamChecker.Classification;
using TftStreamChecker.Output;

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

        RiotId riotId;
        try
        {
            riotId = RiotIdParser.Parse(options.RiotId);
            log.Info("riotId: " + riotId.GameName + "#" + riotId.TagLine + $" ({riotId.Routing})");
        }
        catch (Exception ex)
        {
            log.Error(ex.Message);
            return 1;
        }

        log.Info("twitch: " + (string.IsNullOrWhiteSpace(options.TwitchLogin) ? "(none)" : options.TwitchLogin));
        log.Info("days: " + options.Days);
        log.Info("startTime: " + (options.StartTime ?? "(none)"));
        log.Info("endTime: " + (options.EndTime ?? "(none)"));
        log.Info("eventYear: " + (options.EventYear?.ToString() ?? "(none)"));
        log.Info("eventStart: " + (options.EventStart ?? "(none)"));
        log.Info("eventEnd: " + (options.EventEnd ?? "(none)"));
        log.Info("threshold: " + options.Threshold);

        var window = WindowResolver.Resolve(options);
        if (options.Verbose)
        {
            log.Info("window start: " + new DateTimeOffset(window.StartMs, TimeSpan.Zero).ToString("O"));
            log.Info("window end: " + new DateTimeOffset(window.EndMs, TimeSpan.Zero).ToString("O"));
            log.Info("env riot: " + Mask(env.RiotApiKey));
            log.Info("env twitch id: " + Mask(env.TwitchClientId));
            log.Info("env twitch secret: " + Mask(env.TwitchClientSecret));
        }

        try
        {
            RunSingle(options, env, riotId, window, log).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            log.Error(ex.Message);
            return 1;
        }

        return 0;
    }

    private static async Task RunSingle(
        CliOptions options,
        EnvConfig env,
        RiotId riotId,
        ResolvedWindow window,
        ConsoleLogger log)
    {
        const double defaultBufferHours = 10.0 / 60.0;

        using var httpClient = new HttpClient();
        var retry = new HttpRetryClient(httpClient, log);
        var riot = new RiotClient(retry, log, env.RiotApiKey);
        var twitch = new TwitchClient(retry, log, env.TwitchClientId, env.TwitchClientSecret);

        var puuid = await riot.ResolvePuuid(riotId);
        var matchIds = await riot.ListMatchIds(puuid, riotId.Routing, window.StartMs, window.EndMs);

        var summaries = new List<MatchSummary>();
        foreach (var id in matchIds)
        {
            var detail = await riot.GetMatchDetail(id, riotId.Routing);
            var summary = MatchNormalizer.ToSummary(id, detail);
            if (summary != null) summaries.Add(summary);
        }

        var user = await twitch.GetUserByLogin(options.TwitchLogin);
        var vods = await twitch.ListVodIntervals(user.Id, window.StartMs, window.EndMs, defaultBufferHours);

        var stats = Classifier.Classify(summaries, vods, defaultBufferHours, options.Threshold);
        SummaryPrinter.Print(options.RiotId, riotId, options.TwitchLogin, stats, options.Threshold);
    }

    private static string Mask(string value)
    {
        if (string.IsNullOrEmpty(value)) return "(missing)";
        if (value.Length <= 4) return new string('*', value.Length);
        return new string('*', value.Length - 4) + value[^4..];
    }
}
