using ChinookCheckers.Engine;
using Xunit;

namespace ChinookCheckers.Tests;

// Status strings below are real KingsRow 1.20 output.
public class SearchStatusParserTests
{
    [Fact]
    public void TestParse_SearchLine_ReturnsPvDepthValueAndNodes()
    {
        var info = SearchStatusParser.Parse(
            "value=194,  depth 22/20.1/36,  0.5s,  7563 kN/s,  pv 14x23 27x18 16x23 25-21 12-16");

        Assert.Equal(["14x23", "27x18", "16x23", "25-21", "12-16"], info.Pv);
        Assert.Equal(22, info.Depth);
        Assert.Equal(194, info.Value);
        Assert.Equal(3_781_500, info.Nodes);
    }

    [Fact]
    public void TestParse_StoppedSearchWithGreaterThanValue_ParsesValue()
    {
        var info = SearchStatusParser.Parse("value>208,  depth 25/22.1/39,  0.4s,  7278 kN/s,  pv 14x23 27x18");

        Assert.Equal(208, info.Value);
        Assert.Equal("14x23", info.Pv[0]);
    }

    [Fact]
    public void TestParse_NegativeValue_ParsesSign()
    {
        Assert.Equal(-58, SearchStatusParser.Parse("value=-58,  depth 19/15.8/31,  0.7s,  6675 kN/s,  pv 8-11 22-18").Value);
    }

    [Fact]
    public void TestParse_RootMoveList_UsesFirstMoveAsBest()
    {
        var info = SearchStatusParser.Parse("depth 6; 32-27* (0.250), 31-26* (0.250), 31-27* (0.250), ");

        Assert.Equal(["32-27"], info.Pv);
        Assert.Equal(6, info.Depth);
        Assert.Null(info.Value);
        Assert.Equal(0, info.Nodes);
    }

    [Fact]
    public void TestParse_ForcedMove_ReturnsTheSingleMove()
    {
        Assert.Equal(["22x15"], SearchStatusParser.Parse("depth 6; 22x15* (1.000), ").Pv);
    }

    [Fact]
    public void TestParse_Garbage_ReturnsEmpty()
    {
        var info = SearchStatusParser.Parse("");

        Assert.Empty(info.Pv);
        Assert.Equal(0, info.Depth);
    }
}
