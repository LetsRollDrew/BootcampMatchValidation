using TftStreamChecker.Time;
using Xunit;

namespace TftStreamChecker.Tests;

public class TimeParserTests
{
    [Theory]
    [InlineData("1700000000", 1700000000_000)]
    [InlineData("1700000000000", 1700000000000)]
    [InlineData("2023-12-03T00:00:00Z", 1701561600000)]
    [InlineData("2023-12-03T00:00:00", 1701561600000)]
    public void parses_valid_times(string input, long expectedMs)
    {
        var result = TimeParser.ParseToUnixMs(input);
        Assert.Equal(expectedMs, result);
    }

    [Fact]
    public void returns_null_for_empty()
    {
        Assert.Null(TimeParser.ParseToUnixMs(null));
        Assert.Null(TimeParser.ParseToUnixMs(""));
        Assert.Null(TimeParser.ParseToUnixMs("   "));
    }

    [Fact]
    public void throws_on_bad_input()
    {
        Assert.Throws<FormatException>(() => TimeParser.ParseToUnixMs("not-a-time"));
    }
}
