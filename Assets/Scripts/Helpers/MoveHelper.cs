using System;
using System.Collections;
using System.Diagnostics;
using Chess.Game;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine.UIElements;

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


        // TODO
        public static Move SANMoveToMove(Board board, string SANMove)
        {   
            int colorToMove = board.ColorToMove;
            int SANMoveLength = SANMove.Length;
            // Castling moves
            if (SANMove == "0-0" || SANMove == "0-0-0")
            {
                int offset = SANMove == "0-0" ? 6 : 2;
                int rank = colorToMove == 0 ? 0 : 7;
                return new Move(rank * 8 + 4, rank * 8 + offset, Move.CastleFlag);
            }
                
            // Pawn forward move
            if (SANMoveLength == 2)
            {
                int targetSquare = BoardHelper.SquareIndexFromName(SANMove);
                int sign = colorToMove == 0 ? -1 : 1;
                int startSquare = board.Chessboard[targetSquare + sign * 8] == Piece.Pawn ? targetSquare + sign * 8 : targetSquare + sign * 16;
                return new Move(startSquare, targetSquare, Move.NoFlag);
            }

            if (SANMoveLength == 3)
            {   
                int targetSquare = BoardHelper.SquareIndexFromName(SANMove.Substring(1,2));

                // Pawn capture moves (no ambiguity)
                if (SANMove[0] == 'x')
                {
                    int sign = colorToMove == 0 ? -1 : 1;
                    int startSquare = board.Chessboard[targetSquare + sign * 7] == Piece.Pawn ? targetSquare + sign * 7 : targetSquare + sign * 9;
                    return new Move(startSquare, targetSquare, Move.NoFlag);
                }
                // Piece non-capture moves
                else {
                    int startSquare = FindPiece(board, targetSquare, SANMove[0]);
                    return new Move(startSquare, targetSquare, Move.NoFlag);
                }
                // Pawn promtion
            }

            if (SANMoveLength == 4)
            {
                // Non-pawn captures
                // Disambiguating moves (non-captures)
            }

            if (SANMoveLength == 5)
            {
                // Double disambiguating moves
                // Disambiguating captures
            }

            if (SANMoveLength == 6)
            {
                // Double disambiguating captures
            }

            return Move.NullMove;
        }


        public static int FindPiece(Board board, int targetSquare, char pieceChar)
        {
            int colorToMove = board.ColorToMove;
            int pieceType = Piece.GetPieceTypeFromSymbol(pieceChar);

            for (int i = 0; i < 64; i++)
            {
                if (Piece.PieceType(board.Chessboard[i]) == pieceType && Piece.PieceColor(board.Chessboard[i]) == colorToMove)
                {
                    Span<Move> moves = MoveGenerator.GenerateSquareMoves(board, i, false);
                    foreach (Move move in moves)
                    {
                        if (move.TargetSquare == targetSquare)
                            return i;
                    }
                }
            }

            return -1;
        }


        // Move in the form of e2e4, f6e5, etc.
        public static Move StrMoveToMove(Board board, string strMove)
        {
            int startSquare = BoardHelper.SquareIndexFromName(strMove.Substring(0,2));
            int targetSquare = BoardHelper.SquareIndexFromName(strMove.Substring(2,2));
            int pieceType = Piece.PieceType(board.Chessboard[startSquare]);

            // Double move, en passant move, or promotion
            if (pieceType == 1)
            {      
                // Double mvoe
                if (math.abs(targetSquare - startSquare) == 16)
                    return new Move(startSquare, targetSquare, Move.PawnTwoUpFlag);
                    
                //  Moving to back rank
                else if (targetSquare / 8 == 0 || targetSquare / 8 == 7)
                    // Assume promotion to queen
                    return new Move(startSquare, targetSquare, Move.PromoteToQueenFlag);
                    
                // Moving to an empty square diagonally is always en passant
                else if (board.Chessboard[targetSquare] == 0 && math.abs(targetSquare - startSquare) == 7 || math.abs(targetSquare - startSquare) == 9)
                    return new Move(startSquare, targetSquare, Move.EnPassantCaptureFlag);
            }
            else if (pieceType == 6)
            {
                if (math.abs(targetSquare - startSquare) == 2)
                    return new Move(startSquare, targetSquare, Move.CastleFlag);
            }

            return new Move(startSquare, targetSquare, Move.NoFlag);
        }
    }
}