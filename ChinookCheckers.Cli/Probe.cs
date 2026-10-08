using System.Text;
using ChinookCheckers.Engine;

namespace ChinookCheckers.Cli;

/// <summary>
/// Calibration probe. Puts ONE man on each of the 64 cells of a raw board (no Position, no square
/// mapping involved), asks the engine to move, and prints what came back.
/// </summary>
internal static class Probe
{
    private const int WhiteMan = 5;   // CB_WHITE | CB_MAN
    private const int BlackMan = 6;   // CB_BLACK | CB_MAN
    private const int WhiteColor = 1; // CB_WHITE
    private const int BlackColor = 2; // CB_BLACK

    public static void Run(KingsRowEngine engine)
    {
        Console.WriteLine("Legend: '.' = engine found no move; otherwise delta of flat index (to - from).");
        Console.WriteLine("Rows are flat index / 8, columns are flat index % 8.");

        PrintGrid(engine, "WHITE man, color=1", WhiteMan, WhiteColor);
        PrintGrid(engine, "BLACK man, color=2", BlackMan, BlackColor);
    }

    private static void PrintGrid(KingsRowEngine engine, string title, int piece, int color)
    {
        Console.WriteLine($"\n--- {title} ---");

        var deltas = new SortedSet<int>();

        for (var row = 0; row < 8; row++)
        {
            var line = new StringBuilder();

            for (var col = 0; col < 8; col++)
            {
                var from = row * 8 + col;
                var board = new int[64];
                board[from] = piece;

                engine.GetMoveRaw(board, color, 0.05);

                if (board[from] != 0)
                {
                    line.Append("  . ");
                    continue;
                }

                var to = Array.FindIndex(board, v => v != 0);
                var delta = to - from;
                deltas.Add(delta);
                line.Append($"{delta,3:+0;-0} ");
            }

            Console.WriteLine(line);
        }

        Console.WriteLine($"deltas seen: {string.Join(", ", deltas)}");
    }
}