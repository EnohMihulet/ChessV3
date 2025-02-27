using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Mathematics;
using static Chess.TranspositionTable;

namespace Chess.Core
{
    public class OldSearcher
    {

        const int positiveInfinity = 9999999;
		const int negativeInfinity = -positiveInfinity;
        public const int StartDepth = 5;
        public const int MaxEntensions = 0;
        public int CurrentDepth;
        public static int[] PieceValues = { 0, 1, 3, 3, 5, 9, 100 };

        private readonly TranspositionTable transpositionTable;
        private Evaluation evaluator;

        public OldSearcher(int depth)
        {
            transpositionTable = new TranspositionTable();
            CurrentDepth = depth;
        }

        public struct MoveResult
        {
            public Move move;
            public int score;

            public MoveResult(Move move, int score)
            {
                this.move = move;
                this.score = score;
            }

            // Move result with a null move and score of 0
            public static MoveResult NullZero => new MoveResult(Move.NullMove, 0);

            // Move result with a null move and score of negative infinity
            public static MoveResult NullNegativeInf => new MoveResult(Move.NullMove, negativeInfinity);
        }

        public Move StartSearch(Board board)
        {   
            evaluator = new Evaluation();
            return NegaMax(board, CurrentDepth, negativeInfinity, positiveInfinity, 0).move;
        }

        public MoveResult NegaMax(Board board, int depth, int alpha, int beta, int numExtensions)
        {
            if (depth == 0)
            {
                return new MoveResult 
                {
                    move = Move.NullMove,
                    score = evaluator.Evaluate(board)
                };
            }

            // Return a score of 0 if the position ended to to repetition or the 50 move rule
            if (board.CurrentEndResult == GameResult.EndResult.FiftyMoveRule || board.CurrentEndResult == GameResult.EndResult.Repetition)
                return MoveResult.NullZero;

            if (transpositionTable.GetEvaluation(board.CurrentGameState.ZobristHash) != LookUpFailed)
            {
                Entry entry = transpositionTable.LookUpZobrist(board.CurrentGameState.ZobristHash);

                if (entry.nodeType == LowerBound)
                    alpha = entry.score > alpha ? entry.score : alpha;

                else if (entry.nodeType == UpperBound)
                    beta = entry.score < beta ? entry.score : beta;

                else if (entry.depth >= depth)
                {
                    MoveResult lookUpResult = new MoveResult
                    {
                        move = entry.bestMove,
                        score = entry.score
                    };

                    return lookUpResult;
                }
            }

            MoveResult bestResult = MoveResult.NullNegativeInf;

            int originalAlpha = alpha;

            Span<Move> spanMoves = MoveGenerator.GenerateAllMoves(board, false);

            Move[] allMoves = spanMoves.ToArray();
            Array.Sort(allMoves, new MVVLVASorter(board));

            int count = allMoves.Length;

            if (count == 0) 
            {
                // No moves: a checkmate or stalemate position.
                // If this is stalemate, then return 0
                if (board.CurrentEndResult == GameResult.EndResult.Stalemate)
                    return MoveResult.NullZero;

                // If it is checkmate, return negative infinity
                return MoveResult.NullNegativeInf;
            }

            for (int i = 0; i < count; i ++) 
            {
                Move move = allMoves[i];

                bool isCapture = move.IsCapture(board);

                board.MakeMove(move, true); 

                int nextDepth = depth;
                if (numExtensions <= MaxEntensions)
                {
                    if (isCapture)
                    {
                        numExtensions += 1;
                        nextDepth += 1;
                    }
                    else if (board.IsKingInCheck(board, board.CurrentGameState.ColorToMove))
                    {
                        numExtensions += 1;
                        nextDepth += 1;
                    }
                    else if (Piece.PieceType(board.Chessboard[move.TargetSquare]) == Piece.Pawn && (move.TargetSquare == 1 || move.TargetSquare == 6))
                    {
                        numExtensions += 1;
                        nextDepth += 1;
                    }
                }
                

                MoveResult result = NegaMax(board, nextDepth - 1, -beta, -alpha, numExtensions);
                result.score = -result.score;
                    
                if (result.score >= bestResult.score) 
                {
                    bestResult.score = result.score;
                    bestResult.move = move;
                }

                board.UnMakeMove(move, true);

                // Update alpha
                alpha = math.max(alpha, bestResult.score);

                if (alpha > beta) 
                {
                    break; // prune
                }
            }

            transpositionTable.StoreEntry(new Entry(board.CurrentGameState.ZobristHash, bestResult.move, depth, bestResult.score, SetNodeType(alpha, beta, originalAlpha)));

            return bestResult;
        }
    }
}
