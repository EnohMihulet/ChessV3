using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;

namespace Chess.Core
{
    public class Searcher
    {

        const int positiveInfinity = 9999999;
		const int negativeInfinity = -positiveInfinity;
        public const int StartDepth = 6;
        public int CurrentDepth;
        public static int[] PieceValues = { 0, 1, 3, 3, 5, 9, 100 };

        public Searcher(int depth)
        {
            CurrentDepth = depth;
        }

        public struct MoveResult
        {
            public Move move;
            public int score;
        }

        public Move StartSearch(Board board)
        {            
            return NegaMax(board, CurrentDepth, negativeInfinity, positiveInfinity).move;
        }

        public MoveResult NegaMax(Board board, int depth, int alpha, int beta)
        {
            if (depth == 0)
            {
                return new MoveResult 
                {
                    move = Move.NullMove,
                    score = Evaluation.Evaluate(board)
                };
            }

            MoveResult bestResult = new MoveResult 
            {
                move = Move.NullMove,
                score = negativeInfinity
            };

            Span<Move> spanMoves = MoveGenerator.GenerateAllMoves(board, false);

            Move[] allMoves = spanMoves.ToArray();
            Array.Sort(allMoves, new MVVLVASorter(board));

            int count = allMoves.Length;

            if (count == 0) 
            {
                // No moves: a checkmate or stalemate position.
                // If this is stalemate, then return 0
                if (board.CurrentEndResult == GameResult.EndResult.Stalemate)
                {
                    return new MoveResult 
                    {
                        move = Move.NullMove,
                        score = 0
                    };
                }
                // If it is checkmate, return negative infinity
                return new MoveResult 
                {
                    move = Move.NullMove,
                    score = negativeInfinity
                };
            }

            for (int i = 0; i < count; i ++) {
                Move move = allMoves[i];

                board.MakeMove(move); 

                MoveResult result = NegaMax(board, depth - 1, -beta, -alpha);
                result.score = -result.score;
                    
                if (result.score >= bestResult.score) {
                    bestResult.score = result.score;
                    bestResult.move = move;
                }

                board.UnMakeMove(move);

                // Update alpha
                alpha = Math.Max(alpha, bestResult.score);
                if (alpha > beta) {
                    // Prune
                    break;
                }
            }
            return bestResult;
        }
    }
}
