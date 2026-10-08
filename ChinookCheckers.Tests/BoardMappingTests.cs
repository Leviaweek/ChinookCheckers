using ChinookCheckers.Engine;
using ChinookCheckers.Engine.Models;
using Xunit;

namespace ChinookCheckers.Tests;

public class BoardMappingTests
{
    private static int[] BoardWith(int square)
    {
        var position = PositionParser.Parse($"W:W{square}:B{(square == 1 ? 2 : 1)}");
        var board = new int[64];
        position.WriteToBoard(board);
        return board;
    }

    [Fact]
    public void TestWriteToBoard_EverySquare_RoundTripsThroughBoard()
    {
        for (var square = 1; square <= 32; square++)
        {
            var original = PositionParser.Parse($"W:WK{square}:B{(square == 1 ? 2 : 1)}");
            var board = new int[64];
            original.WriteToBoard(board);

            var restored = Position.WriteFromBoard(board, Side.White);

            Assert.Equal(original.ToPdnString(), restored.ToPdnString());
        }
    }

    [Fact]
    public void TestWriteToBoard_AllSquares_MapToDistinctBoardCells()
    {
        var cells = new HashSet<int>();

        for (var square = 1; square <= 32; square++)
        {
            var board = new int[64];
            PositionParser.Parse($"W:W{square}:B{(square == 1 ? 2 : 1)}").WriteToBoard(board);

            var index = Array.IndexOf(board, (int)(Cell.White | Cell.Man));
            Assert.True(index >= 0, $"Square {square} was not written");
            Assert.True(cells.Add(index), $"Square {square} collides with another square at board index {index}");
        }
    }

    // Anchors verified against the real KingsRow DLL (Cli geometry checks): 4 -> 0, 25 -> 54, 29 -> 63.
    [Theory]
    [InlineData(4, 0)]
    [InlineData(25, 54)]
    [InlineData(29, 63)]
    public void TestWriteToBoard_KnownSquares_MatchEngineGeometry(int square, int expectedIndex)
    {
        var board = BoardWith(square);

        Assert.Equal((int)(Cell.White | Cell.Man), board[expectedIndex]);
    }

    [Fact]
    public void TestWriteToBoard_DirtyBuffer_IsClearedFirst()
    {
        var board = new int[64];
        Array.Fill(board, 99);

        PositionParser.Parse("W:W29:B4").WriteToBoard(board);

        Assert.Equal(2, board.Count(c => c != 0));
    }
}
