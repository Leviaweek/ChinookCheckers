using ChinookCheckers.Engine;
using ChinookCheckers.Engine.Models;
using Xunit;

namespace ChinookCheckers.Tests;

public class PositionParserTests
{
    private const Cell WhiteMan = Cell.White | Cell.Man;
    private const Cell WhiteKing = Cell.White | Cell.King;
    private const Cell BlackMan = Cell.Black | Cell.Man;
    private const Cell BlackKing = Cell.Black | Cell.King;

    /// <summary>
    /// Checks every square (including index 0): listed squares must match, all others must be free.
    /// Catches stray writes and off-by-one errors.
    /// </summary>
    private static void AssertSquares(Position position, params (int Square, Cell Cell)[] expected)
    {
        var expectedMap = expected.ToDictionary(e => e.Square, e => e.Cell);

        for (var square = 1; square <= position.Length; square++)
        {
            var want = expectedMap.GetValueOrDefault(square, Cell.Free);
            Assert.True(want == position[square], $"Square {square}: expected {want}, got {position[square]}");
        }
    }

    // ---------- Valid input ----------

    [Fact]
    public void TestParse_CorrectlyParsesPdnString_ReturnsExpectedPosition()
    {
        const string pdn = "B:W18:B1";

        var position = PositionParser.Parse(pdn);

        Assert.Equal(Cell.White | Cell.Man, position[18]);
        Assert.Equal(Cell.Black | Cell.Man, position[1]);
        Assert.Equal(Side.Black, position.SideToMove);
        Assert.Equal(2, position.PieceCount);
    }

    [Fact]
    public void TestParse_WhiteToMove_SetsSideToMoveWhite()
    {
        var position = PositionParser.Parse("W:W18:B1");

        Assert.Equal(Side.White, position.SideToMove);
    }

    [Fact]
    public void TestParse_StartingPosition_PlacesAllPieces()
    {
        var position = PositionParser.Parse("W:W21,22,23,24,25,26,27,28,29,30,31,32:B1,2,3,4,5,6,7,8,9,10,11,12");

        var expected = Enumerable.Range(1, 12).Select(s => (s, BlackMan))
            .Concat(Enumerable.Range(21, 12).Select(s => (s, WhiteMan)))
            .ToArray();

        AssertSquares(position, expected);
        Assert.Equal(24, position.PieceCount);
    }

    [Fact]
    public void TestParse_KingsOfBothColors_ParsesKingsAndMen()
    {
        var position = PositionParser.Parse("W:WK10,K15,20:BK22,25");

        AssertSquares(position,
            (10, WhiteKing), (15, WhiteKing), (20, WhiteMan),
            (22, BlackKing), (25, BlackMan));
    }

    [Fact]
    public void TestParse_BlackListBeforeWhiteList_ParsesCorrectly()
    {
        var position = PositionParser.Parse("W:B1,2:W31");

        AssertSquares(position, (1, BlackMan), (2, BlackMan), (31, WhiteMan));
    }

    [Fact]
    public void TestParse_BoundarySquares_AcceptsOneAndThirtyTwo()
    {
        var position = PositionParser.Parse("W:W32:B1");

        AssertSquares(position, (32, WhiteMan), (1, BlackMan));
    }

    [Theory]
    [InlineData("W:W:B1", 1)]
    [InlineData("W:W1:B", 1)]
    [InlineData("W:W:B", 0)]
    [InlineData("W:B:W", 0)]
    public void TestParse_EmptyPieceList_IsAccepted(string pdn, int expectedPieceCount)
    {
        var position = PositionParser.Parse(pdn);

        Assert.Equal(expectedPieceCount, position.PieceCount);
    }

    [Fact]
    public void TestParse_LeadingZerosInSquare_AreAccepted()
    {
        // Pins current behaviour (the PDN FEN grammar allows leading zeros).
        var position = PositionParser.Parse("W:W05:B01");

        AssertSquares(position, (5, WhiteMan), (1, BlackMan));
    }

    // ---------- Invalid input ----------

    [Fact]
    public void TestParse_NullInput_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => PositionParser.Parse(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("W")]
    [InlineData("W:")]
    [InlineData("W:W1")]
    [InlineData("W:W1:B1:W2")]
    [InlineData("W:W21::B1")]
    [InlineData("W:W21:B1:")]
    [InlineData("W::W21:B1")]
    public void TestParse_InvalidStructure_ThrowsFormatException(string pdn)
    {
        Assert.Throws<FormatException>(() => PositionParser.Parse(pdn));
    }

    [Theory]
    [InlineData("X:W1:B1")]
    [InlineData("w:W1:B1")]
    [InlineData(" W:W1:B1")]
    [InlineData("WB:W1:B1")]
    [InlineData(":W1:B1")]
    public void TestParse_InvalidSideToMove_ThrowsFormatException(string pdn)
    {
        Assert.Throws<FormatException>(() => PositionParser.Parse(pdn));
    }

    [Theory]
    [InlineData("W:W1:W2")]
    [InlineData("W:B1:B2")]
    public void TestParse_BothListsSameColor_ThrowsFormatException(string pdn)
    {
        Assert.Throws<FormatException>(() => PositionParser.Parse(pdn));
    }

    [Theory]
    [InlineData("W:X1:B2")]
    [InlineData("W:w1:B2")]
    [InlineData("W:1:B2")]
    public void TestParse_InvalidPieceColor_ThrowsFormatException(string pdn)
    {
        Assert.Throws<FormatException>(() => PositionParser.Parse(pdn));
    }

    [Theory]
    [InlineData("W::B1")]
    [InlineData("W:W1:")]
    public void TestParse_EmptyPieceListSegment_ThrowsFormatException(string pdn)
    {
        Assert.Throws<FormatException>(() => PositionParser.Parse(pdn));
    }

    [Theory]
    [InlineData("W:W0:B1")]
    [InlineData("W:W33:B1")]
    [InlineData("W:W-1:B1")]
    [InlineData("W:W+5:B1")]
    [InlineData("W:W 5:B1")]
    [InlineData("W:W5 :B1")]
    [InlineData("W:W5a:B1")]
    [InlineData("W:W5K:B1")]
    [InlineData("W:W2K1:B1")]
    [InlineData("W:Wk5:B1")]
    [InlineData("W:WK:B1")]
    [InlineData("W:WKK5:B1")]
    [InlineData("W:W21,:B1")]
    [InlineData("W:W,21:B1")]
    [InlineData("W:W21,,22:B1")]
    [InlineData("W:W99999999999:B1")]
    [InlineData("W:W\u0662\u0661:B1")] // Arabic-Indic digits
    public void TestParse_InvalidSquare_ThrowsFormatException(string pdn)
    {
        Assert.Throws<FormatException>(() => PositionParser.Parse(pdn));
    }

    [Theory]
    [InlineData("W:W5,5:B1")]
    [InlineData("W:W5,K5:B1")]
    [InlineData("W:WK5,5:B1")]
    [InlineData("W:W5:B5")]
    [InlineData("W:W5:BK5")]
    public void TestParse_DuplicateSquare_ThrowsFormatException(string pdn)
    {
        Assert.Throws<FormatException>(() => PositionParser.Parse(pdn));
    }
}