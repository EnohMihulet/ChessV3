using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Chess.Core
{
    public class GameState
    {
        // Side to move
        public int ColorToMove;
        // Castling rights
        public readonly int CastlingRights;
        public readonly int CapturedPieceType;
		public readonly int EnPassantFile; // -1 is no file 0-7 are files
		public readonly int HalfmoveClock;
        public readonly int FullmoveCount;
        // Zobrist hash
        public ulong ZobristHash;
        public const int ClearWhiteKingsideMask = 0b1110;
		public const int ClearWhiteQueensideMask = 0b1101;
		public const int ClearBlackKingsideMask = 0b1011;
		public const int ClearBlackQueensideMask = 0b0111;

        public bool WhiteKingSideCastlingRight => (CastlingRights & 0x1) != 0;
        public bool WhiteQueenSideCastlingRight => (CastlingRights & 0x2) != 0;
        public bool BlackKingSideCastlingRight => (CastlingRights & 0x4) != 0;
        public bool BlackQueenSideCastlingRight => (CastlingRights & 0x8) != 0;

        public const int StartColorToMove = 0;
        public const int StartCapturedPieceType = 0;
        public const int StartEnPassantFile = -1;
        public const int StartHalfMoveClock = 0;
        public const int StartFullMoveCount = 0;


        public GameState(int colorToMove, int castlingRights, int enPassantCaptureFile, int capturedPieceType,
            int halfmoveClock, int fullmoveCount, ulong zobristHash)
        {
            ColorToMove = colorToMove;
            CastlingRights = castlingRights;
            EnPassantFile = enPassantCaptureFile;
            CapturedPieceType = capturedPieceType;
            HalfmoveClock = halfmoveClock;
            FullmoveCount = fullmoveCount;
            ZobristHash = zobristHash;
        }

        public void ChangeColorToMove() => ColorToMove = (ColorToMove == 0) ? 1 : 0;

        public static bool CanWhiteCastleKingSide(int castlingRights) => (castlingRights & 0x1) != 0;
        public static bool CanWhiteCastleQueenSide(int castlingRights) => (castlingRights & 0x2) != 0;
        public static bool CanBlackCastleKingSide(int castlingRights) => (castlingRights & 0x4) != 0;
        public static bool CanBlackCastleQueenSide(int castlingRights) => (castlingRights & 0x8) != 0;
    }
}