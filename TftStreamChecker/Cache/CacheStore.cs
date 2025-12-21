using System.Text.Json;

namespace TftStreamChecker.Cache;

public class CacheStore
{
    private readonly string _root;
    private readonly JsonSerializerOptions _jsonOptions;

    public CacheStore(string? root = null, JsonSerializerOptions? options = null)
    {
        _root = Path.GetFullPath(root ?? ".cache");
        _jsonOptions = options ?? CreateOptions();
        Directory.CreateDirectory(_root);
    }

    public string GetPath(params string[] parts)
    {
        var all = new List<string> { _root };
        all.AddRange(parts);
        return Path.Combine(all.ToArray());
    }

    public T? Read<T>(string relativePath)
    {
        var path = GetPath(relativePath);
        if (!File.Exists(path)) return default;
        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }
        catch
        {
            return default;
        }
    }

    public void Write<T>(string relativePath, T value)
    {
        var path = GetPath(relativePath);
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var json = JsonSerializer.Serialize(value, _jsonOptions);
        File.WriteAllText(path, json);
    }

    private static JsonSerializerOptions CreateOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
    }
}
