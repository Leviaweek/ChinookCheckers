namespace ChinookCheckers.Engine;

public static class CellExtensions
{
    public static bool IsFree(this Cell cell) => cell == Cell.Free;
    public static bool IsWhite(this Cell cell) => (cell & Cell.White) != 0;
    public static bool IsBlack(this Cell cell) => (cell & Cell.Black) != 0;
    public static bool IsMan(this Cell cell) => (cell & Cell.Man) != 0;
    public static bool IsKing(this Cell cell) => (cell & Cell.King) != 0;
}