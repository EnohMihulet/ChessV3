using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using UnityEditor.Hardware;

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

        // ADD FUNCTION TO CHECK FOR SUFFICIENT MATERIAL
    }
}