namespace ChinookCheckers.Engine;

public sealed class Position
{
    public Side SideToMove { get; init; }
    public Cell[] Squares { get; init; } = new Cell[33];
    public int PieceCount => Squares.Count(c => !c.IsFree());
}