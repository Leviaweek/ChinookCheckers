namespace ChinookCheckers.Engine;

[Flags]
public enum Cell: byte
{
    Free = 0,
    
    White = 1,
    Black = 2,
    
    Man = 4,
    King = 8
}