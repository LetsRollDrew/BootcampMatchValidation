using TftStreamChecker.Classification;
using TftStreamChecker.Models;
using Xunit;

namespace TftStreamChecker.Tests;

public class ClassifierTests
{
    [Fact]
    public void counts_on_and_off_stream_with_buffer()
    {
        var matches = new List<MatchSummary>
        {
            new() { MatchId = "m1", StartMs = 1000, EndMs = 2000 },
            new() { MatchId = "m2", StartMs = 10_000, EndMs = 12_000 }
        };

        var vods = new List<VodInterval>
        {
            new() { Id = "v1", StartMs = 0, EndMs = 1500 }
        };

        var stats = Classifier.Classify(matches, vods, bufferHours: 0.001, threshold: 0.5);

        Assert.Equal(1, stats.OnStream);
        Assert.Equal(1, stats.OffStream);
        Assert.Equal(0, stats.Unknown);
        Assert.False(stats.Pass);
    }

    [Fact]
    public void passes_on_threshold_known_only()
    {
        var matches = new List<MatchSummary>
        {
            new() { MatchId = "m1", StartMs = 1000, EndMs = 2000 },
        };

        var vods = new List<VodInterval>
        {
            new() { Id = "v1", StartMs = 0, EndMs = 5000 }
        };

        var stats = Classifier.Classify(matches, vods, bufferHours: 0, threshold: 0.5);
        Assert.True(stats.Pass);
        Assert.Equal(1.0, stats.PctKnown);
        Assert.Equal(1.0, stats.PctTotal);
    }

    [Fact]
    public void handles_empty_matches()
    {
        var stats = Classifier.Classify(Array.Empty<MatchSummary>(), Array.Empty<VodInterval>(), bufferHours: 0, threshold: 0.5);
        Assert.Equal(0, stats.Total);
        Assert.False(stats.Pass);
    }
}
