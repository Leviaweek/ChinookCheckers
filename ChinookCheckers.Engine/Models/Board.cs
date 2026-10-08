namespace ChinookCheckers.Engine.Models;

/// <summary>Geometry of the 32 playable squares: square 1 is row 0 col 1, row 0 is Black's back rank.</summary>
internal static class Board
{
    public static int Row(int square) => (square - 1) / 4;

    public static int Col(int square) => (square - 1) % 4 * 2 + (Row(square) % 2 == 0 ? 1 : 0);

    /// <summary>Returns the square at row/col, or 0 when off the board or on a light square.</summary>
    public static int SquareAt(int row, int col)
    {
        if (row is < 0 or > 7 || col is < 0 or > 7 || (row % 2 == 0) != (col % 2 == 1)) return 0;
        return row * 4 + col / 2 + 1;
    }

    public static bool IsPromotionRow(Side side, int square) => Row(square) == (side == Side.White ? 0 : 7);
}
