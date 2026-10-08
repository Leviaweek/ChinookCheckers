namespace ChinookCheckers.Engine.Models;

public enum NativeMoveResult
{
    Draw,
    Win,
    Loss,
    Unknown
}

public class MoveResult
{
    public required NativeMoveResult Result { get; init; }
    public required Position Before { get; init; }   
    public required Position After { get; init; }
    public required string Status { get; init; }
}