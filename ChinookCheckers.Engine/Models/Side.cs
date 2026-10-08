namespace ChinookCheckers.Engine.Models;

public enum Side { White = 1, Black = 2 }

public static class SideExtensions
{
    public static Side Opposite(this Side side) => side == Side.White ? Side.Black : Side.White;
    public static string ToPdnString(this Side side) => side == Side.White ? "W" : "B";
}