using System.Collections.Generic;

namespace Chess.Core
{
    public class MVVLVASorter : IComparer<Move>
    {
        public Board board;

        public MVVLVASorter(Board board)
        {
            this.board = board;
        }

        public int Compare(Move x, Move y)
        {
            // Negative return means x before y, 0 if x are equal in ordering, and positive if y before x

            // First check if move is capture. Captured moves are always prioritized over non-captures
            bool xIsCapture = x.IsCapture(board);
            bool yIsCapture = y.IsCapture(board);

            if (!xIsCapture && !yIsCapture)
                return 0;

            if (xIsCapture && !yIsCapture)
                return -1;

            if (!xIsCapture && yIsCapture)
                return 1;

            // Second, if both moves are captures, prioritzed the capture with a higher value victim
            int xCapturedPiece = x.IsEnPassantCapture ? 1 : Piece.PieceType(board.Chessboard[x.TargetSquare]);
            int yCapturedPiece = y.IsEnPassantCapture ? 1 : Piece.PieceType(board.Chessboard[y.TargetSquare]);

            if (xCapturedPiece > yCapturedPiece)
                return -1;
            
            if (yCapturedPiece > xCapturedPiece)
                return 1;

            // Third, if both captured pieces are the same value, then prioritze the lower value attacking piece
            int xAttackingPiece = Piece.PieceType(board.Chessboard[x.StartSquare]);
            int yAttackingPiece = Piece.PieceType(board.Chessboard[y.StartSquare]);

            if (xAttackingPiece < yAttackingPiece)
                return -1;

            if (yAttackingPiece < xAttackingPiece)
                return 1;

            return 0;
        }
    }
}