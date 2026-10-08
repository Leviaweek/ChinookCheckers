using ChinookCheckers.Engine.Models;

namespace ChinookCheckers.Engine;

/// <summary>English draughts rules: captures are mandatory, multi-jumps continue to the end,
/// a man that reaches the last row becomes a king and its move ends there.</summary>
public static class MoveGenerator
{
    public static IReadOnlyList<Move> LegalMoves(Position position)
    {
        var side = position.SideToMove;
        var own = side == Side.White ? Cell.White : Cell.Black;
        var captures = new List<Move>();
        var steps = new List<Move>();

        for (var square = 1; square <= 32; square++)
        {
            var piece = position[square];
            if ((piece & own) == 0) continue;

            CollectCaptures(position, side, piece, square, square, [square], [], captures);

            foreach (var (dRow, dCol) in Directions(side, piece))
            {
                var target = Board.SquareAt(Board.Row(square) + dRow, Board.Col(square) + dCol);
                if (target != 0 && position[target].IsFree())
                    steps.Add(new Move { Path = [square, target], Captured = [] });
            }
        }

        return captures.Count > 0 ? captures : steps;
    }

    /// <summary>Finds the legal move written like "22-18", "22x15" or "22x15x8". For captures the short
    /// form with only the first and last square is accepted when it identifies a single move.</summary>
    public static Move? FindMove(Position position, string pdnMove)
    {
        var squares = new List<int>();
        foreach (var part in pdnMove.Split(['-', 'x'], StringSplitOptions.TrimEntries))
        {
            if (!int.TryParse(part, out var square)) return null;
            squares.Add(square);
        }

        if (squares.Count < 2) return null;

        var legal = LegalMoves(position);
        var exact = legal.FirstOrDefault(m => m.Path.SequenceEqual(squares) && m.ContainsCapture == pdnMove.Contains('x'));
        if (exact != null) return exact;

        if (!pdnMove.Contains('x')) return null;
        var byEnds = legal.Where(m => m.ContainsCapture && m.From == squares[0] && m.To == squares[^1]).ToList();
        return byEnds.Count == 1 ? byEnds[0] : null;
    }

    public static bool IsLegal(Position position, string pdnMove) => FindMove(position, pdnMove) != null;

    private static IEnumerable<(int Row, int Col)> Directions(Side side, Cell piece)
    {
        var forward = side == Side.White ? -1 : 1;
        if (piece.IsKing()) yield return (-forward, -1);
        if (piece.IsKing()) yield return (-forward, 1);
        yield return (forward, -1);
        yield return (forward, 1);
    }

    // The origin square counts as empty while the piece is in flight; a piece can't be jumped twice.
    private static void CollectCaptures(Position position, Side side, Cell piece, int origin, int current,
        List<int> path, List<int> captured, List<Move> result)
    {
        var extended = false;
        var opponent = side == Side.White ? Cell.Black : Cell.White;

        foreach (var (dRow, dCol) in Directions(side, piece))
        {
            var over = Board.SquareAt(Board.Row(current) + dRow, Board.Col(current) + dCol);
            var land = Board.SquareAt(Board.Row(current) + 2 * dRow, Board.Col(current) + 2 * dCol);

            if (over == 0 || land == 0 || (position[over] & opponent) == 0 || captured.Contains(over)) continue;
            if (!position[land].IsFree() && land != origin) continue;

            extended = true;
            path.Add(land);
            captured.Add(over);

            if (piece.IsMan() && Board.IsPromotionRow(side, land))
                result.Add(new Move { Path = path.ToArray(), Captured = captured.ToArray() });
            else
                CollectCaptures(position, side, piece, origin, land, path, captured, result);

            path.RemoveAt(path.Count - 1);
            captured.RemoveAt(captured.Count - 1);
        }

        if (!extended && captured.Count > 0)
            result.Add(new Move { Path = path.ToArray(), Captured = captured.ToArray() });
    }
}
