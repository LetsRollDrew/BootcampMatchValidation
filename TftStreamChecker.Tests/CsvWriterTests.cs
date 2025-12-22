using System.Text;
using TftStreamChecker.Models;
using TftStreamChecker.Output;
using Xunit;

namespace TftStreamChecker.Tests;

public class CsvWriterTests
{
    [Fact]
    public void writes_header_once_and_appends_rows()
    {
        var path = Path.Combine(Path.GetTempPath(), $"csvwriter_{Guid.NewGuid()}.csv");
        try
        {
            var riotId = new RiotId { GameName = "Tree Otter", TagLine = "3500", Routing = "AMERICAS" };
            var stats = new MatchStats
            {
                Total = 2,
                OnStream = 1,
                OffStream = 1,
                Unknown = 0,
                PctTotal = 0.5,
                Pass = false
            };

            CsvWriter.Append(path, "Tree Otter", riotId, "treeotter", stats);
            CsvWriter.Append(path, "Tree Otter", riotId, "treeotter", stats, "Name Changed");

            var lines = File.ReadAllLines(path, Encoding.UTF8);
            Assert.Equal(3, lines.Length); // header + 2 rows
            Assert.StartsWith("Name,GameName,TagLine", lines[0]);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
