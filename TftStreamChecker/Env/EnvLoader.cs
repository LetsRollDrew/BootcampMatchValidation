using System.Collections.Generic;
using System.IO;

namespace TftStreamChecker.Env;

public record EnvConfig(string RiotApiKey, string TwitchClientId, string TwitchClientSecret);

public static class EnvLoader
{
    public static EnvConfig Load(string? path = null)
    {
        var envPath = path ?? Path.Combine(Directory.GetCurrentDirectory(), ".env");
        if (File.Exists(envPath))
        {
            LoadFile(envPath);
        }

        var riot = ReadVar("RIOT_API_KEY") ?? string.Empty;
        var twitchId = ReadVar("TWITCH_CLIENT_ID") ?? string.Empty;
        var twitchSecret = ReadVar("TWITCH_CLIENT_SECRET") ?? string.Empty;

        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(riot)) missing.Add("RIOT_API_KEY");
        if (string.IsNullOrWhiteSpace(twitchId)) missing.Add("TWITCH_CLIENT_ID");
        if (string.IsNullOrWhiteSpace(twitchSecret)) missing.Add("TWITCH_CLIENT_SECRET");
        if (missing.Count > 0)
        {
            throw new InvalidOperationException("missing env vars: " + string.Join(", ", missing));
        }

        return new EnvConfig(riot.Trim(), twitchId.Trim(), twitchSecret.Trim());
    }

    private static void LoadFile(string path)
    {
        foreach (var rawLine in File.ReadAllLines(path))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

            var idx = line.IndexOf('=');
            if (idx <= 0) continue;

            var key = line[..idx].Trim();
            var value = line[(idx + 1)..].Trim();
            if (key.Length == 0) continue;

            Environment.SetEnvironmentVariable(key, value);
        }
    }

    private static string? ReadVar(string key) => Environment.GetEnvironmentVariable(key);
}
