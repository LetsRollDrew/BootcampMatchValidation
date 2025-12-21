using TftStreamChecker.Parsing;
using Xunit;

namespace TftStreamChecker.Tests;

public class RiotIdParserTests
{
    [Fact]
    public void parses_basic_id()
    {
        var id = RiotIdParser.Parse("Tree Otter#3500");
        Assert.Equal("Tree Otter", id.GameName);
        Assert.Equal("3500", id.TagLine);
        Assert.Equal("AMERICAS", id.Routing);
    }

    [Theory]
    [InlineData("KR", "ASIA")]
    [InlineData("JP", "ASIA")]
    [InlineData("EUW", "EUROPE")]
    [InlineData("EUNE", "EUROPE")]
    [InlineData("TR", "EUROPE")]
    [InlineData("ME1", "EUROPE")]
    [InlineData("RU", "EUROPE")]
    [InlineData("EUW2", "EUROPE")]
    [InlineData("OCE", "SEA")]
    [InlineData("SG2", "SEA")]
    [InlineData("TW2", "SEA")]
    [InlineData("VN2", "SEA")]
    [InlineData("NA1", "AMERICAS")]
    public void infers_routing(string tag, string expected)
    {
        var routing = RiotIdParser.InferRouting(tag);
        Assert.Equal(expected, routing);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("NoHash")]
    [InlineData("#Tag")]
    [InlineData("Name#")]
    public void rejects_bad_inputs(string input)
    {
        Assert.Throws<ArgumentException>(() => RiotIdParser.Parse(input));
    }
}
