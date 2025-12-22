using System.Text.Json;
using TftStreamChecker.Cli;
using TftStreamChecker.Models;

namespace TftStreamChecker.Participants;

public static class ParticipantParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true
    };

    public static async Task<List<Participant>> ReadAsync(CliOptions options, CancellationToken cancellationToken = default)
    {
        var json = await ReadRawInput(options, cancellationToken);
        return Parse(json);
    }

    public static List<Participant> Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidOperationException("input is empty");

        List<Participant>? direct = null;
        try
        {
            direct = JsonSerializer.Deserialize<List<Participant>>(json, JsonOptions);
        }
        catch
        {
            // ignore and try envelope
        }

        if (direct != null)
        {
            return direct.Where(p => p != null).ToList();
        }

        ParticipantEnvelope? envelope = null;
        try
        {
            envelope = JsonSerializer.Deserialize<ParticipantEnvelope>(json, JsonOptions);
        }
        catch
        {
            // ignore
        }

        if (envelope?.Participants != null)
        {
            return envelope.Participants.Where(p => p != null).ToList();
        }

        throw new InvalidOperationException("input must be array or object with Participants[]");
    }

    private static async Task<string> ReadRawInput(CliOptions options, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(options.InputPath))
        {
            var path = Path.GetFullPath(options.InputPath!);
            return await File.ReadAllTextAsync(path, cancellationToken);
        }

        using var reader = new StreamReader(Console.OpenStandardInput());
        return await reader.ReadToEndAsync();
    }

    private class ParticipantEnvelope
    {
        public List<Participant>? Participants { get; set; }
        public string? RankUpdatedText { get; set; }
        public string? NextElimTime { get; set; }
    }
}
