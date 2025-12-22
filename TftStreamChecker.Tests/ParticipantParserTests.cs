using TftStreamChecker.Cli;
using TftStreamChecker.Participants;
using Xunit;

namespace TftStreamChecker.Tests;

public class ParticipantParserTests
{
    [Fact]
    public void parses_array_input()
    {
        var json = "[{\"Name\":\"Player1\",\"Rank\":1}]";
        var participants = ParticipantParser.Parse(json);
        Assert.Single(participants);
        Assert.Equal("Player1", participants[0].Name);
        Assert.Equal(1, participants[0].Rank);
    }

    [Fact]
    public void parses_envelope_input()
    {
        var json = "{\"Participants\":[{\"Name\":\"Player2\",\"Rank\":2}]}";
        var participants = ParticipantParser.Parse(json);
        Assert.Single(participants);
        Assert.Equal("Player2", participants[0].Name);
        Assert.Equal(2, participants[0].Rank);
    }

    [Fact]
    public void throws_on_invalid_input()
    {
        Assert.Throws<InvalidOperationException>(() => ParticipantParser.Parse("not-json"));
    }
}
