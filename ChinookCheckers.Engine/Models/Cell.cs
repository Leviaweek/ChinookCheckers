namespace ChinookCheckers.Engine.Models;

[Flags]
public enum Cell: int
{
    Free = 0,
    
    White = 1,
    Black = 2,
    
    Man = 4,
    King = 8
}