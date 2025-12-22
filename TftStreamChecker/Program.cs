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
using TftStreamChecker.Cache;
using TftStreamChecker.Participants;

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

        RiotId? riotId = null;
        if (string.IsNullOrWhiteSpace(options.InputPath))
        {
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
        }

        log.Info("twitch: " + (string.IsNullOrWhiteSpace(options.TwitchLogin) ? "(none)" : options.TwitchLogin));
        log.Info("days: " + options.Days);
        log.Info("startTime: " + (options.StartTime ?? "(none)"));
        log.Info("endTime: " + (options.EndTime ?? "(none)"));
        log.Info("eventYear: " + (options.EventYear?.ToString() ?? "(none)"));
        log.Info("eventStart: " + (options.EventStart ?? "(none)"));
        log.Info("eventEnd: " + (options.EventEnd ?? "(none)"));
        log.Info("threshold: " + options.Threshold);
        log.Info("concurrency: " + options.Concurrency);

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
        RiotId? riotId,
        ResolvedWindow window,
        ConsoleLogger log)
    {
        using var httpClient = new HttpClient();
        var retry = new HttpRetryClient(httpClient, log);
        var cache = new CacheStore();
        var riot = new RiotClient(retry, log, env.RiotApiKey, cache: cache, useCache: options.UseCache);
        var twitch = new TwitchClient(retry, log, env.TwitchClientId, env.TwitchClientSecret, cache: cache, useCache: options.UseCache);

        // batch mode if input provided
        if (!string.IsNullOrWhiteSpace(options.InputPath))
        {
            var participants = await ParticipantParser.ReadAsync(options);
            var sorted = participants
                .OrderBy(p => p.Rank ?? double.PositiveInfinity)
                .Take(30)
                .ToList();
            foreach (var participant in sorted)
            {
                await ProcessOne(options, env, riot, twitch, window, log, participant);
            }
            return;
        }

        if (riotId == null)
        {
            log.Error("riotId is required for single mode");
            return;
        }

        var single = new Participant { Name = options.RiotId, RankUrl = options.RiotId.Replace("#", "-") };
        await ProcessOne(options, env, riot, twitch, window, log, single);
    }

    private static async Task ProcessOne(
        CliOptions options,
        EnvConfig env,
        RiotClient riot,
        TwitchClient twitch,
        ResolvedWindow window,
        ConsoleLogger log,
        Participant participant)
    {
        const double defaultBufferHours = 10.0 / 60.0;

        var riotIdText = ExtractRiotId(participant.RankUrl) ?? options.RiotId;
        if (string.IsNullOrWhiteSpace(riotIdText))
        {
            log.Info("SKIP " + (participant.Name ?? "(unknown)") + ": missing riot id");
            return;
        }

        RiotId riotId;
        try
        {
            riotId = RiotIdParser.Parse(riotIdText);
        }
        catch (Exception ex)
        {
            log.Info("SKIP " + (participant.Name ?? "(unknown)") + ": " + ex.Message);
            return;
        }
        var twitchLogin = string.IsNullOrWhiteSpace(options.TwitchLogin)
            ? ExtractTwitchLogin(participant.Socials)
            : options.TwitchLogin;

        var participantWindow = BuildParticipantWindow(participant, window);
        if (participantWindow == null)
        {
            log.Info("SKIP " + (participant.Name ?? "(unknown)") + ": window excluded by elimination");
            return;
        }

        if (string.IsNullOrWhiteSpace(twitchLogin))
        {
            log.Info("SKIP " + (participant.Name ?? "(unknown)") + ": missing twitch login");
            return;
        }

        string puuid;
        try
        {
            puuid = await riot.ResolvePuuid(riotId);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("riot id not found", StringComparison.OrdinalIgnoreCase))
        {
            var displayName = participant.Name ?? (riotId.GameName + "#" + riotId.TagLine);
            LogNameChanged(displayName, riotId, twitchLogin);
            var zeroStats = new MatchStats();
            CsvWriter.Append(options.OutputCsv ?? string.Empty, displayName, riotId, twitchLogin, zeroStats, "Name Changed");
            return;
        }
        var matchIds = await riot.ListMatchIds(puuid, riotId.Routing, participantWindow.Value.StartMs, participantWindow.Value.EndMs);

        var summaries = await AsyncMap(matchIds, Math.Max(1, options.Concurrency), async id =>
        {
            var detail = await riot.GetMatchDetail(id, riotId.Routing);
            return MatchNormalizer.ToSummary(id, detail);
        });
        var summariesFiltered = summaries.Where(s => s != null).Cast<MatchSummary>().ToList();

        var user = await twitch.GetUserByLogin(twitchLogin);
        var vods = await twitch.ListVodIntervals(user.Id, participantWindow.Value.StartMs, participantWindow.Value.EndMs, defaultBufferHours);

        var stats = Classifier.Classify(summariesFiltered, vods, defaultBufferHours, options.Threshold);
        var displayNameFinal = participant.Name ?? (riotId.GameName + "#" + riotId.TagLine);
        SummaryPrinter.Print(displayNameFinal, riotId, twitchLogin, stats, options.Threshold);
        CsvWriter.Append(options.OutputCsv ?? string.Empty, displayNameFinal, riotId, twitchLogin, stats);
    }

    private static string? ExtractRiotId(string? rankUrl)
    {
        if (string.IsNullOrWhiteSpace(rankUrl)) return null;
        try
        {
            var uri = new Uri(rankUrl);
            var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var last = segments.LastOrDefault();
            if (string.IsNullOrWhiteSpace(last)) return null;
            var decoded = Uri.UnescapeDataString(last);
            var parts = decoded.Split('-', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) return null;
            var tag = parts[^1].Trim();
            var name = string.Join("-", parts.Take(parts.Length - 1)).Trim();
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(tag)) return null;
            return name + "#" + tag;
        }
        catch
        {
            return null;
        }
    }

    private static string ExtractTwitchLogin(List<Social>? socials)
    {
        if (socials == null) return string.Empty;
        foreach (var s in socials)
        {
            if (string.IsNullOrWhiteSpace(s?.LinkUri)) continue;
            if (!s.LinkUri.Contains("twitch.tv", StringComparison.OrdinalIgnoreCase)) continue;
            var uri = new Uri(s.LinkUri);
            var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length > 0) return segments[0];
        }
        return string.Empty;
    }

    private static void LogNameChanged(string name, RiotId riotId, string twitchLogin)
    {
        Console.WriteLine($"=== {name} ({riotId.GameName}#{riotId.TagLine}) twitch:{twitchLogin} ===");
        Console.WriteLine("Result: Name Changed (Riot ID not found)");
        Console.WriteLine("");
    }

    private static (long StartMs, long EndMs)? BuildParticipantWindow(Participant participant, ResolvedWindow baseWindow)
    {
        var start = Math.Max(baseWindow.StartMs, baseWindow.EventStartMs);
        var end = baseWindow.EndMs;
        if (baseWindow.EventEndMs.HasValue)
        {
            end = Math.Min(end, baseWindow.EventEndMs.Value);
        }

        if (participant.Eliminated == true && participant.DayEliminated.HasValue && participant.DayEliminated.Value > 0)
        {
            var elimEnd = baseWindow.EventStartMs + participant.DayEliminated.Value * 86_400_000L - 1;
            end = Math.Min(end, elimEnd);
        }

        if (end <= start) return null;
        return (start, end);
    }

    private static async Task<List<T>> AsyncMap<T>(IReadOnlyList<string> items, int concurrency, Func<string, Task<T>> func)
    {
        var results = new T[items.Count];
        using var sem = new SemaphoreSlim(concurrency);
        var tasks = new List<Task>();
        for (var i = 0; i < items.Count; i++)
        {
            var index = i;
            await sem.WaitAsync();
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    results[index] = await func(items[index]);
                }
                finally
                {
                    sem.Release();
                }
            }));
        }

        await Task.WhenAll(tasks);
        return results.ToList();
    }

    private static string Mask(string value)
    {
        if (string.IsNullOrEmpty(value)) return "(missing)";
        if (value.Length <= 4) return new string('*', value.Length);
        return new string('*', value.Length - 4) + value[^4..];
    }
}
