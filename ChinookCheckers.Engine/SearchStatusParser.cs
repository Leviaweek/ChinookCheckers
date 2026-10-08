using System.Globalization;
using System.Text.RegularExpressions;

namespace ChinookCheckers.Engine;

/// <summary>
/// Parses the text KingsRow writes into the getmove status buffer. Two shapes are seen:
/// a search line ("value=194,  depth 22/20.1/36,  0.5s,  7563 kN/s,  pv 14x23 27x18 ...") and
/// a root move list ("depth 6; 32-27* (0.250), 31-26* (0.250), ..."), where the first move is the chosen one.
/// </summary>
internal static partial class SearchStatusParser
{
    public sealed record Info(IReadOnlyList<string> Pv, int Depth, long Nodes, int? Value);

    [GeneratedRegex(@"\d+(?:[-x]\d+)+")]
    private static partial Regex MoveRegex();

    [GeneratedRegex(@"depth (\d+)")]
    private static partial Regex DepthRegex();

    [GeneratedRegex(@"value[=<>]?(-?\d+)")]
    private static partial Regex ValueRegex();

    [GeneratedRegex(@"(\d+(?:\.\d+)?)s,")]
    private static partial Regex SecondsRegex();

    [GeneratedRegex(@"(\d+(?:\.\d+)?) kN/s")]
    private static partial Regex KnpsRegex();

    public static Info Parse(string status)
    {
        var depth = DepthRegex().Match(status) is { Success: true } d ? int.Parse(d.Groups[1].Value) : 0;
        int? value = ValueRegex().Match(status) is { Success: true } v ? int.Parse(v.Groups[1].Value) : null;

        // Nodes are not printed; derive them from speed and elapsed time when both are present.
        var nodes = 0L;
        if (SecondsRegex().Match(status) is { Success: true } s && KnpsRegex().Match(status) is { Success: true } k)
            nodes = (long)(double.Parse(s.Groups[1].Value, CultureInfo.InvariantCulture)
                           * double.Parse(k.Groups[1].Value, CultureInfo.InvariantCulture) * 1000);

        var pvIndex = status.IndexOf("pv ", StringComparison.Ordinal);
        var moves = pvIndex >= 0
            ? MoveRegex().Matches(status, pvIndex).Select(m => m.Value).ToList()
            : MoveRegex().Matches(status).Take(1).Select(m => m.Value).ToList();

        return new Info(moves, depth, nodes, value);
    }
}
