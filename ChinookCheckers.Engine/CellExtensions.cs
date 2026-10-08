using ChinookCheckers.Engine.Models;

namespace ChinookCheckers.Engine;

public static class CellExtensions
{
    public static bool IsFree(this Cell cell) => cell == Cell.Free;
    public static bool IsWhite(this Cell cell) => (cell & Cell.White) != 0;
    public static bool IsBlack(this Cell cell) => (cell & Cell.Black) != 0;
    public static bool IsMan(this Cell cell) => (cell & Cell.Man) != 0;
    public static bool IsKing(this Cell cell) => (cell & Cell.King) != 0;

    public static string RoleToPdnString(this Cell cell)
    {
        if (cell.IsFree()) return "";
        
        // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
        // Cell is a [Flags] enum: only specific bit combinations are valid.
        return (cell & (Cell.Man | Cell.King)) switch
        {
            Cell.Man => "",
            Cell.King => "K",
            _ => throw new ArgumentOutOfRangeException(nameof(cell), "Invalid cell role.")
        };
    }
    
    public static string ColorToPdnString(this Cell cell)
    {
        if (cell.IsFree()) return "";
        
        // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
        // Cell is a [Flags] enum: only specific bit combinations are valid.
        return (cell & (Cell.White | Cell.Black)) switch
        {
            Cell.White => "W",
            Cell.Black => "B",
            _ => throw new ArgumentOutOfRangeException(nameof(cell), "Invalid cell color.")
        };
    }
}