using System;
using Unity.Mathematics;

namespace Chess.Core
{
    public class Searcher
    {

        const int positiveInfinity = 9999999;
		const int negativeInfinity = -positiveInfinity;
        public const int StartDepth = 5;
        public int CurrentDepth;

        public Searcher(int depth, bool isWhite)
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

            Span<Move> allMoves = MoveGenerator.GenerateAllMoves(board, false);
            int count = allMoves.Length;

            if (count == 0) 
            {
                // No moves: a checkmate or stalemate position.
                // If this is a losing position for current player:

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
                if (alpha >= beta) {
                    // Prune
                    break;
                }
            }
            return bestResult;
        }
    }  
}
