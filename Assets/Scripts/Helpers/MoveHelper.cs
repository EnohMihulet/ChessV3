using System;
using System.Collections;
using Unity.Mathematics;
using Unity.VisualScripting;

namespace Chess.Core
{
    class MoveHelper
    {
        /*
            Not a complete implementation
            There is ambiguity that has to be resolved for the notation to be completely correct
        */
        public static string MoveToSANMove(Board board, Move move)
        {
            if (move == null)
                return "NULL";

            int startSquare = move.StartSquare;
            int targetSquare = move.TargetSquare;
            
            // Castling moves
            if (move.IsCastling)
            {
                int x = startSquare - targetSquare;
                if (x < 0)
                    return "0-0"; // Kingside
                else
                    return "0-0-0"; // Queenside
            }

            string SANMove = "";
            int pieceType = Piece.PieceType(board.Chessboard[startSquare]);

            Coordinate startCoor = new Coordinate(startSquare);
            Coordinate targetCoor = new Coordinate(targetSquare);

            char startFile = BoardHelper.fileNames[startCoor.file];
            string startRank = (startCoor.rank + 1).ToString();

            char targetFile = BoardHelper.fileNames[targetCoor.file];
            string targetRank = (targetCoor.rank + 1).ToString();
            
            bool isCapture = board.Chessboard[targetSquare] != 0 || move.IsEnPassantCapture ? true : false;

            // Non-pawn
            if (pieceType != 1)
            {
                SANMove += Piece.GetSymbolFromPiece(pieceType);
            }
            // Pawns
            else if (isCapture)
            {   
                // Add pawn file if it is a capture
                SANMove += startFile;
            }
            // Captures
            if (isCapture)
                SANMove += 'x';

            SANMove += targetFile;
            SANMove += targetRank;

            return SANMove;
        }
    }
}