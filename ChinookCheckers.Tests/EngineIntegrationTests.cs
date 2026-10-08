using System.Diagnostics;
using ChinookCheckers.Engine;
using ChinookCheckers.Engine.Models;
using Xunit;

namespace ChinookCheckers.Tests;

/// <summary>Loads the real KingsRow DLL once for all tests; does nothing when the env vars are missing.</summary>
public sealed class EngineFixture : IDisposable
{
    public KingsRowEngine? Engine { get; }

    public EngineFixture()
    {
        if (!EngineIntegrationTests.Configured) return;

        Engine = new KingsRowEngine(EngineIntegrationTests.Dll!);
        Engine.Configure(EngineIntegrationTests.Db!, 256);
        Engine.RequireCommand("set book 0");
        Engine.WarmUp();
    }

    public void Dispose() => Engine?.Dispose();
}

// Run with CHINOOK_DLL=<path to Kingsrow64.dll> CHINOOK_DB=<path to wld databases>; otherwise every test is skipped.
public class EngineIntegrationTests(EngineFixture fixture) : IClassFixture<EngineFixture>
{
    public static string? Dll => Environment.GetEnvironmentVariable("CHINOOK_DLL");
    public static string? Db => Environment.GetEnvironmentVariable("CHINOOK_DB");
    public static bool Configured => !string.IsNullOrEmpty(Dll) && !string.IsNullOrEmpty(Db);

    private const string Skip = nameof(Configured);
    private const string NoEngine = "Set CHINOOK_DLL and CHINOOK_DB to run engine integration tests";
    private const string Midgame = "B:W18,19,22,25,27,28,30,32:B1,5,6,7,10,12,14,16";

    private KingsRowEngine Engine => fixture.Engine!;

    [Fact(Skip = NoEngine, SkipUnless = Skip, SkipType = typeof(EngineIntegrationTests))]
    public void TestHandshake_ReportsKingsRowEnglishCheckers()
    {
        Assert.Contains("Kingsrow", Engine.Name());
        Assert.Equal("2", Engine.ProtocolVersion());
        Assert.Equal("21", Engine.GameType());
    }

    // Positions with exactly one legal move pin down board orientation and colour constants.
    [Theory(Skip = NoEngine, SkipUnless = Skip, SkipType = typeof(EngineIntegrationTests))]
    [InlineData("W:W29:B4", "B:W25:B4")]
    [InlineData("B:W29:B4", "W:W29:B8")]
    [InlineData("W:W22:B4,18", "B:W15:B4")]
    [InlineData("W:W5:B28", "B:WK1:B28")]
    public async Task TestGetMove_SingleLegalMove_ReturnsExpectedPosition(string before, string expectedAfter)
    {
        var result = await Engine.GetMoveAsync(Position.Parse(before), 0.2, 2.0, TestContext.Current.CancellationToken);

        Assert.Equal(expectedAfter, result.After.ToPdnString());
    }

    [Fact(Skip = NoEngine, SkipUnless = Skip, SkipType = typeof(EngineIntegrationTests))]
    public async Task TestSearch_Middlegame_ReturnsLegalMove()
    {
        var result = await Engine.SearchAsync(Position.Parse(Midgame), new SearchLimits(0, 300, 600), TestContext.Current.CancellationToken);

        Assert.NotNull(result.LegalMove);
        Assert.False(result.TablebaseHit);
        Assert.True(result.Depth > 0);
    }

    [Fact(Skip = NoEngine, SkipUnless = Skip, SkipType = typeof(EngineIntegrationTests))]
    public async Task TestGetMove_HardLimit_StopsLongSearchEarly()
    {
        var sw = Stopwatch.StartNew();

        await Engine.GetMoveAsync(Position.Parse(Midgame), 5.0, 0.4, TestContext.Current.CancellationToken);

        Assert.True(sw.ElapsedMilliseconds < 1500, $"{sw.ElapsedMilliseconds} ms for soft=5.0 s, hard=0.4 s");
    }

    [Fact(Skip = NoEngine, SkipUnless = Skip, SkipType = typeof(EngineIntegrationTests))]
    public async Task TestSearch_ThreePieceEndgame_IsTablebaseHitUnder100Ms()
    {
        var sw = Stopwatch.StartNew();

        var result = await Engine.SearchAsync(Position.Parse("B:W29:B4,K12"), new SearchLimits(12, 30, 45), TestContext.Current.CancellationToken);

        Assert.True(result.TablebaseHit);
        Assert.True(sw.ElapsedMilliseconds < 100, $"{sw.ElapsedMilliseconds} ms");
    }

    // The pool loads its own DLL copies (Kingsrow64.worker0/1.dll next to the original).
    [Fact(Skip = NoEngine, SkipUnless = Skip, SkipType = typeof(EngineIntegrationTests))]
    public async Task TestPool_ParallelRequests_AllAnswered()
    {
        using var pool = EnginePool.Create(Dll!, Db!, 256);

        var results = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ =>
            pool.RunAsync(Position.Parse(Midgame), new SearchLimits(0, 300, 350), TestContext.Current.CancellationToken)));

        Assert.Equal(2, pool.LiveWorkers);
        Assert.All(results, r => Assert.NotNull(r.LegalMove));
    }
}
