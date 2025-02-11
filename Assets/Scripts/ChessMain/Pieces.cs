using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Chess.Core
{
    public class Piece
    {
        
        // Piece Types
        public const int None = 0;
		public const int Pawn = 1;
		public const int Knight = 2;
		public const int Bishop = 3;
		public const int Rook = 4;
		public const int Queen = 5;
		public const int King = 6;


        // Piece Colors
		public const int White = 0;
		public const int Black = 8;

        // Pieces
		public const int WhitePawn = Pawn | White; // 1
		public const int WhiteKnight = Knight | White; // 2
		public const int WhiteBishop = Bishop | White; // 3
		public const int WhiteRook = Rook | White; // 4
		public const int WhiteQueen = Queen | White; // 5
		public const int WhiteKing = King | White; // 6

		public const int BlackPawn = Pawn | Black; // 9
		public const int BlackKnight = Knight | Black; // 10
		public const int BlackBishop = Bishop | Black; // 11
		public const int BlackRook = Rook | Black; // 12
		public const int BlackQueen = Queen | Black; // 13
		public const int BlackKing = King | Black; // 14

        public static bool IsWhite(int piece) => (piece & 8) == 0;
        public static bool IsBlack(int piece) => (piece & 8) != 0;
        public static int PieceColor(int piece) => IsWhite(piece) ? 0 : 1;
        public static int PieceType(int piece) => piece & 7;
        public static bool IsNone(int piece) => piece == None;

        public static int MakePiece(int pieceType, int pieceColor)
        {
            int piece = pieceColor == 0 ? pieceType | White : pieceType | Black;
            return piece;
        }

        // Get piece value from piece
        // Useful for board evaluation
        public static int GetPieceValue(int piece) 
        {
            int pieceType = PieceType(piece);
            switch (pieceType) 
            {
                case Pawn:   return 100;
                case Knight: return 300;
                case Bishop: return 300;
                case Rook:   return 500;
                case Queen:  return 900;
                case King:   return 10000; 
                default:     return 0;
            }
        }

        public static char GetSymbolFromPiece(int piece)
        {
            int pieceType = PieceType(piece);
            char symbol = pieceType switch
            {
                Pawn   => 'P',
                Knight => 'N',
                Bishop => 'B',
                Rook   => 'R',
                Queen  => 'Q',
                King   => 'K',
                _      => ' ',
            };
            // If the piece is black, make the symbol lowercase:
            symbol = IsWhite(piece) ? symbol : char.ToLower(symbol);
            return symbol;
        }

		public static int GetPieceTypeFromSymbol(char symbol)
		{
			symbol = char.ToUpper(symbol);
			return symbol switch
			{
				'R' => Rook,
				'N' => Knight,
				'B' => Bishop,
				'Q' => Queen,
				'K' => King,
				'P' => Pawn,
				_ => None
			};
		}

        public static bool PiecesAreSameColor(int Piece1, int Piece2)
        {
            // Returns false if either "pieces" are empty squares
            if (Piece1 == Piece.None || Piece2 == Piece.None) 
                return false;
            return PieceColor(Piece1) == PieceColor(Piece2) ? true : false;

        }

        public static bool PiecesAreDifferentColor(int Piece1, int Piece2)
        {
            // Returns false if either "pieces" are empty squares
            if (Piece1 == Piece.None || Piece2 == Piece.None) 
                return false;
            return PieceColor(Piece1) == PieceColor(Piece2) ? false : true;
        }
    }
}

