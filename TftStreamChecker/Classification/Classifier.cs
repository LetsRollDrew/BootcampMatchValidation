using TftStreamChecker.Models;

namespace TftStreamChecker.Classification;

public static class Classifier
{
    public static MatchStats Classify(
        IEnumerable<MatchSummary> matches,
        IEnumerable<VodInterval> vods,
        double bufferHours,
        double threshold)
    {
        var bufferMs = bufferHours > 0 ? bufferHours * 3600 * 1000 : 0;
        var vodList = vods.ToList();

        var onStream = 0;
        var offStream = 0;
        var unknown = 0;

        foreach (var match in matches)
        {
            var isOn = vodList.Any(v =>
                match.StartMs >= v.StartMs - bufferMs &&
                match.StartMs <= v.EndMs + bufferMs);
            if (isOn) onStream++;
            else offStream++;
        }

        var total = onStream + offStream + unknown;
        var known = total - unknown;
        var pctKnown = known > 0 ? onStream / (double)known : 0;
        var pctTotal = total > 0 ? onStream / (double)total : 0;

        return new MatchStats
        {
            Total = total,
            OnStream = onStream,
            OffStream = offStream,
            Unknown = unknown,
            PctKnown = pctKnown,
            PctTotal = pctTotal,
            Pass = pctKnown >= threshold
        };
    }
}
