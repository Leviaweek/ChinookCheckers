namespace ChinookCheckers.Engine.Models;

public sealed class Position
{
    public Side SideToMove { get; init; }
    private readonly Cell[] _squares = new Cell[32];
    public int PieceCount => _squares.Count(c => !c.IsFree());
    public int Length => _squares.Length;
    
    /// <summary>
    /// Gets or sets the cell at the given PDN square.
    /// </summary>
    /// <param name="square">Square number in PDN numbering, 1 to 32.</param>
    /// <exception cref="IndexOutOfRangeException">The square is outside 1 to 32.</exception>
    public Cell this[int square]
    {
        get => _squares[ToIndex(square)];
        internal set => _squares[ToIndex(square)] = value;
    }

    private static int ToIndex(int square) => square - 1;
    private static int ToSquare(int index) => index + 1;

    public static Position Parse(string pdn) => PositionParser.Parse(pdn);

    /// <summary>
    /// Writes the position to a board [64] ([8 x 8]) buffer, where each square is represented by a int.
    ///       1   2   3   4
    ///     5   6   7   8
    ///     9  10  11  12
    ///    13  14  15  16
    ///     17  18  19  20
    ///    21  22  23  24
    ///     25  26  27  28
    ///    29  30  31  32
    ///
    /// The buffer must contain 64 elements, all initialized to zero.
    /// </summary>
    /// <param name="boardBuffer"></param>
    public void WriteToBoard(Span<int> boardBuffer)
    {
        if (boardBuffer.Length != 64)
            throw new ArgumentException("Board buffer must be of length 64.", nameof(boardBuffer));
        
        for (var index = 0; index < 32; index++)
        {
            var cell = _squares[index];

            if (cell.IsFree())
                continue;

            /*var row = index / 4;
            var col = index % 4 * 2 + row % 2;*/
            
            boardBuffer[ToBoardIndex(index)] = (int)cell;
        }
    }
    
    private static int ToBoardIndex(int index)
    {
        var row = index / 4;
        var col = index % 4 * 2 + (row % 2 == 0 ? 1 : 0);
        var x = 7 - col;
        return x * 8 + row;
    }

    public void PrintBoard()
    {
        Span<int> board = stackalloc int[64];
        WriteToBoard(board);

        for (var row = 0; row < 8; row++)
        {
            for (var col = 0; col < 8; col++)
            {
                var x = 7 - col;
                var cellValue = board[x * 8 + row];
                Console.Write(cellValue + " ");
            }
            Console.WriteLine();
        }
    }
    
    public static Position WriteFromBoard(ReadOnlySpan<int> boardBuffer, Side sideToMove)
    {
        if (boardBuffer.Length != 64)
            throw new ArgumentException("Board buffer must be of length 64.", nameof(boardBuffer));
        
        var position = new Position { SideToMove = sideToMove };

        for (var index = 0; index < 32; index++)
        {
            position._squares[index] = (Cell)boardBuffer[ToBoardIndex(index)];
        }

        return position;
    }

    public string ToPdnString()
    {
        return $"{SideToMove.ToPdnString()}:{FormatPieces(Cell.White)}:{FormatPieces(Cell.Black)}";
    }
    
    private string FormatPieces(Cell color)
    {
        var pieces = new List<string>();

        for (var square = 0; square < _squares.Length; square++)
        {
            var cell = _squares[square];

            if ((cell & color) == 0) continue;
            pieces.Add(cell.RoleToPdnString() + ToSquare(square));
        }

        return color.ColorToPdnString() + string.Join(',', pieces);
    }
}

public sealed class Move
{
    public required IReadOnlyList<int> Path { get; init; }
    public required IReadOnlyList<int> Captured { get; init; }
    public int From => Path[0];
    public int To => Path[^1];
    public bool ContainsCapture => Captured.Count > 0;
    public override string ToString() => string.Join(ContainsCapture ? "x" : "-", Path);

}