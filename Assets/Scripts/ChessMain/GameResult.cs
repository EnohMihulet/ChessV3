using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using UnityEditor.Hardware;
using UnityEngine;

namespace Chess.Core
{
    public class GameResult
    {
	    public enum EndResult
        {
            InProgress,
            WhiteIsMated,
            BlackIsMated,
            Stalemate,
            Repetition,
            FiftyMoveRule,
            InsufficientMaterial
        }

        public static bool IsCheckmate(Board board)
        {
            // Checkmate if there are no moves and king is in check 
            int color = board.CurrentGameState.ColorToMove;

            Move[] moveArray = new Move[MoveGenerator.MaxMoves];
            Span<Move> moves = moveArray.AsSpan();
            int moveCount;

            MoveGenerator.GenerateAllMoves(board, moves, false, out moveCount);

            // No legal moves
            if (moveCount == 0)
            {
                // Check for king in check
                if (board.IsKingInCheck(board, color))
                    return true;
            }
            return false;
        }


        public static bool IsDraw(Board board)
        {
            // 50 MOVE RULE
            if (FiftyMoveRule(board.CurrentGameState))
                return true;

            // STALEMATE
            // Stalemate if there are no moves and king is not in check
            int color = board.CurrentGameState.ColorToMove;

            Move[] moveArray = new Move[MoveGenerator.MaxMoves];
            Span<Move> moves = moveArray.AsSpan();
            int moveCount;

            MoveGenerator.GenerateAllMoves(board, moves, false, out moveCount);

            // No legal moves
            if (moveCount == 0)
            {
                // Check for king in check
                if (!board.IsKingInCheck(board, color))
                    return true;
            }
            else
            {
                return !IsSufficientMaterial(board);
            }
            
            // REPETITION
            return IsDrawByRepetition(board);
        }

        public static EndResult CurrentGameResult(Board board)
        {   
            int color = board.CurrentGameState.ColorToMove;

            Move[] moveArray = new Move[MoveGenerator.MaxMoves];
            Span<Move> moves = moveArray.AsSpan();
            int moveCount = 0;

            MoveGenerator.GenerateAllMoves(board, moves, false, out moveCount);

            // No legal moves
            if (moveCount == 0)
            {
                if (board.IsKingInCheck(board, color)) {
                    return color == 0 ? EndResult.WhiteIsMated : EndResult.BlackIsMated;
                }
                return  EndResult.Stalemate;
            }
            else
            {
                if (!IsSufficientMaterial(board))
                    return EndResult.InsufficientMaterial;
            }

            if (IsDrawByRepetition(board))
                return EndResult.Repetition;
            
            if (FiftyMoveRule(board.CurrentGameState))
                return EndResult.FiftyMoveRule;

            return EndResult.InProgress;
        }

        public static bool IsDrawByRepetition(Board board)
        {
            Stack<ulong> repetitionHistory = board.RepetitionPositionHistory;

            if (repetitionHistory.Count < 3)
                return false;
            
            ulong currentZobrist = board.CurrentGameState.ZobristHash;

            int count = 0;

            foreach (ulong hash in repetitionHistory)
            {
                if (hash == currentZobrist)
                    count++;

                if (count >= 3)
                    return true;
            }

            return false;
        }

        public static bool FiftyMoveRule(GameState gameState)
        {
            return gameState.HalfmoveClock >= 100 ? true : false;
        }

        public bool IsGameOver(Board board)
        {
            if (CurrentGameResult(board) != EndResult.InProgress)
                return true;
            return false;
        }

        // Checks for sufficient material
        public static bool IsSufficientMaterial(Board board)
        {
            // No pawns on the board
            if (board.AllPieces[1].Count == 0 && board.AllPieces[9].Count == 0)
            {   
                // At least one queen on the board
                if (board.AllPieces[5].Count != 0 || board.AllPieces[13].Count != 0)
                    return true;
                // At least one rook on the board
                if (board.AllPieces[4].Count != 0 || board.AllPieces[12].Count != 0)
                    return true;

                int whiteKnight = board.AllPieces[2].Count;
                int blackKnight = board.AllPieces[10].Count;
                int whiteBishop = board.AllPieces[3].Count;
                int blackBishop = board.AllPieces[11].Count;
                // Check the double knight and no bishop endgame
                if (whiteBishop == 0 && blackBishop == 0)
                {
                    // No bishops and one side has two knight and the other has zero
                    if ((whiteKnight <= 2 && blackKnight == 0) || (blackKnight <= 2 && whiteKnight == 0))
                        return false;
                }
                // Both sides have at most a bishop or a knight
                if (whiteKnight + whiteBishop <= 1 && blackKnight + blackBishop <= 1)
                    return false;
                return true;
            }
            return true;
        }
    }
}