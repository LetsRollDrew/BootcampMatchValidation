using System.Text.Json;
using TftStreamChecker.Cache;
using Xunit;

namespace TftStreamChecker.Tests;

public class CacheStoreTests
{
    [Fact]
    public void read_missing_returns_default()
    {
        using var temp = new TempDir();
        var cache = new CacheStore(temp.Path);

        var result = cache.Read<Dictionary<string, string>>("missing.json");

        Assert.Null(result);
    }

    [Fact]
    public void write_and_read_round_trips()
    {
        using var temp = new TempDir();
        var cache = new CacheStore(temp.Path);
        var data = new Dictionary<string, string> { ["foo"] = "bar" };

        cache.Write("nested/file.json", data);
        var read = cache.Read<Dictionary<string, string>>("nested/file.json");

        Assert.NotNull(read);
        Assert.Equal("bar", read!["foo"]);
    }

    [Fact]
    public void bad_json_returns_default()
    {
        using var temp = new TempDir();
        var path = Path.Combine(temp.Path, "bad.json");
        Directory.CreateDirectory(temp.Path);
        File.WriteAllText(path, "{ not-json }");

        var cache = new CacheStore(temp.Path);
        var read = cache.Read<Dictionary<string, string>>("bad.json");

        Assert.Null(read);
    }

    private sealed class TempDir : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        public TempDir()
        {
            Directory.CreateDirectory(Path);
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(Path, recursive: true);
            }
            catch
            {
                // ignore
            }
        }
    }
}
