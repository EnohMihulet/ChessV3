using System.Collections;
using System.Collections.Generic;

namespace Chess.Core {
    public static class Zobrist
    {
        // Zobrist Hash is a unique ulong containing the information of any chess position
        private static readonly ulong Seed = 1234512345;
        public static readonly ulong[,] PieceSquareArray = new ulong[15, 64]; // 0 is no piece
		public static readonly ulong[] CastlingRights = new ulong[16];
		public static readonly ulong[] EnPassantFile = new ulong[9]; // 0 is no en passant
		public static readonly ulong SideToMove;

        public const int ZobristKeyCount = 922;
        

        static Zobrist()
        {
            // Create a local PRNG state
            RKISS.RanCtx ctx = default;
            RKISS.RanInit(ref ctx, Seed);

            // Fill the piece-square keys array
            for (int i = 0; i < 15; i++)
            {
                for (int j = 0; j < 64; j++)
                {
                    PieceSquareArray[i,j] = RKISS.RanVal(ref ctx);
                }
            }
            // Fill castling rights keys array
            for (int i = 0; i < CastlingRights.Length; i++)
            {
                CastlingRights[i] = RKISS.RanVal(ref ctx);
            }
            // Fill en passant file keys array
            for (int i = 0; i < EnPassantFile.Length; i++)
            {
                EnPassantFile[i] = RKISS.RanVal(ref ctx);
            }
            SideToMove = RKISS.RanVal(ref ctx);
            
        }

        public static ulong ZobristHash(Board board)
        {   
            ulong zobristHash = board.CurrentGameState.ZobristHash;
            for (int i = 0; i < 64; i++)
            {  
                zobristHash ^= PieceSquareArray[board.Chessboard[i], i];
            }
            zobristHash ^= CastlingRights[board.CurrentGameState.CastlingRights];
            zobristHash ^= EnPassantFile[board.CurrentGameState.EnPassantFile + 1];
            zobristHash ^= SideToMove;
            return zobristHash;
        }
    }
}