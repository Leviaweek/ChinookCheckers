using ChinookCheckers.Api;
using Xunit;

namespace ChinookCheckers.Tests;

public class LevelsTests
{
    [Theory]
    [InlineData("weak", 100)]
    [InlineData("MEDIUM", 250)]
    [InlineData("strong", 450)]
    [InlineData(null, 250)]
    public void TestLimitsFor_Level_ReturnsLevelSoftTime(string? level, int softMs)
    {
        Assert.Equal(softMs, Levels.LimitsFor(Levels.Normalize(level), null).SoftTimeMs);
    }

    [Fact]
    public void TestLimitsFor_StrongHardTime_StaysUnderSixHundredMs()
    {
        Assert.True(Levels.LimitsFor("strong", new SuggestLimits(null, null, 1200)).HardTimeMs < 600);
    }

    [Fact]
    public void TestLimitsFor_RequestedLimits_CanOnlyTighten()
    {
        var limits = Levels.LimitsFor("medium", new SuggestLimits(5, 50, 60));

        Assert.Equal((5, 50, 60), (limits.MaxDepth, limits.SoftTimeMs, limits.HardTimeMs));
        Assert.Equal(250, Levels.LimitsFor("medium", new SuggestLimits(null, 5000, null)).SoftTimeMs);
    }

    [Fact]
    public void TestNormalize_UnknownLevel_Throws422()
    {
        var error = Assert.Throws<ApiException>(() => Levels.Normalize("godlike"));

        Assert.Equal(422, error.StatusCode);
    }

    [Fact]
    public void TestLimitsFor_NonPositiveLimit_Throws422()
    {
        Assert.Equal(422, Assert.Throws<ApiException>(() => Levels.LimitsFor("weak", new SuggestLimits(null, 0, null))).StatusCode);
    }
}
