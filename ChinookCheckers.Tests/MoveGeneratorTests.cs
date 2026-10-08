using ChinookCheckers.Engine;
using ChinookCheckers.Engine.Models;
using Xunit;

namespace ChinookCheckers.Tests;

public class MoveGeneratorTests
{
    private static string[] Moves(string pdn) =>
        MoveGenerator.LegalMoves(Position.Parse(pdn)).Select(m => m.ToString()).Order(StringComparer.Ordinal).ToArray();

    [Fact]
    public void TestLegalMoves_StartingPosition_ReturnsSevenMoves()
    {
        const string black = "B:W21,22,23,24,25,26,27,28,29,30,31,32:B1,2,3,4,5,6,7,8,9,10,11,12";
        const string white = "W:W21,22,23,24,25,26,27,28,29,30,31,32:B1,2,3,4,5,6,7,8,9,10,11,12";

        Assert.Equal(["10-14", "10-15", "11-15", "11-16", "12-16", "9-13", "9-14"], Moves(black));
        Assert.Equal(["21-17", "22-17", "22-18", "23-18", "23-19", "24-19", "24-20"], Moves(white));
    }

    [Fact]
    public void TestLegalMoves_CaptureAvailable_SimpleMovesAreNotLegal()
    {
        Assert.Equal(["22x15"], Moves("W:W22,25:B18"));
    }

    [Fact]
    public void TestLegalMoves_MultiJump_ContinuesToTheEnd()
    {
        var moves = MoveGenerator.LegalMoves(Position.Parse("W:W31:B27,19"));

        var move = Assert.Single(moves);
        Assert.Equal("31x24x15", move.ToString());
        Assert.Equal([27, 19], move.Captured);
    }

    [Fact]
    public void TestLegalMoves_ManMovesOnlyForward()
    {
        Assert.Equal(["29-25"], Moves("W:W29:B4"));
        Assert.Equal(["4-8"], Moves("B:W29:B4"));
    }

    [Fact]
    public void TestLegalMoves_King_MovesBothDirections()
    {
        Assert.Equal(["14-10", "14-17", "14-18", "14-9"], Moves("W:WK14:B4"));
    }

    [Fact]
    public void TestLegalMoves_NoPieces_ReturnsEmpty()
    {
        Assert.Empty(MoveGenerator.LegalMoves(Position.Parse("W:W5:B1,9,10")));
    }

    [Fact]
    public void TestApply_ManReachingLastRow_BecomesKingAndSideFlips()
    {
        var position = Position.Parse("W:W6:B32");

        var next = position.Apply(MoveGenerator.FindMove(position, "6-1")!);

        Assert.Equal("B:WK1:B32", next.ToPdnString());
    }

    [Fact]
    public void TestApply_Capture_RemovesCapturedPieces()
    {
        var position = Position.Parse("W:W31:B27,19");

        var next = position.Apply(MoveGenerator.FindMove(position, "31x24x15")!);

        Assert.Equal("B:W15:B", next.ToPdnString());
        Assert.Equal(1, next.PieceCount);
    }

    [Fact]
    public void TestApply_ManCapturingIntoLastRow_StopsAndPromotes()
    {
        // 10x3 reaches Black's back rank; as a king it could continue over 8, but the move ends on promotion.
        var position = Position.Parse("W:W10:B7,8");

        var move = Assert.Single(MoveGenerator.LegalMoves(position));

        Assert.Equal("10x3", move.ToString());
        Assert.Equal("B:WK3:B8", position.Apply(move).ToPdnString());
    }

    [Theory]
    [InlineData("W:W22,25:B18", "22x15", true)]
    [InlineData("W:W22,25:B18", "25-21", false)]
    [InlineData("W:W31:B27,19", "31x24x15", true)]
    [InlineData("W:W31:B27,19", "31x15", true)]
    [InlineData("W:W29:B4", "29-25", true)]
    [InlineData("W:W29:B4", "29-24", false)]
    [InlineData("W:W29:B4", "4-8", false)]
    [InlineData("W:W29:B4", "nonsense", false)]
    [InlineData("W:W29:B4", "25", false)]
    public void TestIsLegal_Move_ReturnsExpected(string pdn, string move, bool expected)
    {
        Assert.Equal(expected, MoveGenerator.IsLegal(Position.Parse(pdn), move));
    }

    [Fact]
    public void TestFindMoveLeadingTo_ResultingPosition_ReturnsThatMove()
    {
        var position = Position.Parse("W:W31:B27,19");
        var after = Position.Parse("B:W15:B");

        Assert.Equal("31x24x15", MoveGenerator.FindMoveLeadingTo(position, after)!.ToString());
        Assert.Null(MoveGenerator.FindMoveLeadingTo(position, Position.Parse("B:W24:B19")));
    }
}
