using System.Globalization;
using ChinookCheckers.Engine.Models;

namespace ChinookCheckers.Engine;

/// <summary>
/// Parses a position from a PDN (Portable Draughts Notation) string.
/// </summary>
internal static class PositionParser
{
    public static Position Parse(string pdn)
    {
        ArgumentNullException.ThrowIfNull(pdn);
        
        if (pdn.Split(':', 2) is not [var rawSideToMove, var rawPieces])
        {
            throw new FormatException("Invalid PDN format.");
        }
        
        var sideToMove = rawSideToMove switch
        {
            "W" => Side.White,
            "B" => Side.Black,
            _ => throw new FormatException("Invalid side to move in PDN.")
        };
        
        var position = new Position { SideToMove = sideToMove };
        
        if (rawPieces.Split(':') is not [var rawPieces1, var rawPieces2])
        {
            throw new FormatException("Invalid PDN format.");
        }
        
        var color1 = ParsePieces(rawPieces1, position);
        var color2 = ParsePieces(rawPieces2, position);

        if (color1 == color2)
        {
            throw new FormatException("Both piece lists in PDN have the same color.");
        }
        
        return position;
    }
    
    private static Cell ParsePieces(string rawPieces, Position position)
    {
        var rawPiecesSpan = rawPieces.AsSpan();
        
        if (rawPiecesSpan.IsEmpty)
            throw new FormatException("Empty piece list in PDN.");
        
        var color = rawPiecesSpan[0] switch 
        {
            'W' => Cell.White,
            'B' => Cell.Black,
            _ => throw new FormatException("Invalid piece color in PDN.")
        };
        
        var piecesSpan = rawPiecesSpan[1..];

        if (piecesSpan.IsEmpty)
            return color;
        
        foreach (var rawPiece in piecesSpan.Split(','))
        {
            var pieceSpan = piecesSpan[rawPiece];
            
            if (pieceSpan.IsEmpty)
            {
                throw new FormatException("Invalid piece square in PDN.");
            }
            
            var isKing = pieceSpan[0] == 'K';
            
            if (isKing) pieceSpan = pieceSpan[1..];
            
            if (!int.TryParse(pieceSpan,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out var square) ||
                square < 1 ||
                square > 32)
            {
                throw new FormatException("Invalid piece square in PDN.");
            }
            
            if (position[square] != Cell.Free)
            {
                throw new FormatException("Duplicate piece square in PDN.");
            }
            
            position[square] = color | (isKing ? Cell.King : Cell.Man);
        }

        return color;
    }
}