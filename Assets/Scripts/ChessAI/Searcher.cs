using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using static Chess.TranspositionTable;

namespace Chess.Core
{
    public class Searcher
    {

        const int positiveInfinity = 9999999;
		const int negativeInfinity = -positiveInfinity;
        public const int MaxEntensions = 0;
        public int CurrentSearchDepth;
        public Move BestMoveThisIteration;
        public int BestEvalThisIteration;

        public bool SearchCanceled = false;
        Stopwatch SearchTimer;


        public Move BestMove;
        public int BestEval;
        public Board board;
        private readonly TranspositionTable transpositionTable;
        private Evaluation evaluator;

        public Searcher()
        {
            this.transpositionTable = new TranspositionTable();
            this.evaluator = new Evaluation();
        }

        public Move StartSearch(Board board)
        {   
            this.board = board;
            CurrentSearchDepth = 1;
            SearchCanceled = false;
            SearchTimer = Stopwatch.StartNew();

            BestMove = BestMoveThisIteration = Move.NullMove;
            BestEval = BestEvalThisIteration = negativeInfinity;

            IterativeDeepeningSearch();
            
            UnityEngine.Debug.Log(CurrentSearchDepth);
            return BestMove;
        }

        void IterativeDeepeningSearch()
        {
            for (int depth = 1; depth < 100; depth++)
            {
                CurrentSearchDepth = depth;

                Search(depth, negativeInfinity, positiveInfinity);

                if (!SearchCanceled)
                {
                    BestEval = BestEvalThisIteration;
                    BestMove = BestMoveThisIteration;
                }

                if (SearchTimer.ElapsedMilliseconds >= 3000)
                {
                    SearchCanceled = true;
                    return;
                }
            }
        }

        int Search(int ply, int alpha, int beta)
        {
            // Searched cancelled, exit search
            if (SearchCanceled)
            {
                return 0;
            }

            if (CurrentSearchDepth > 0) {
                // Draw by repetition or FiftyMoveRule, return score for draw (0)
                if (board.CurrentEndResult == GameResult.EndResult.Repetition || board.CurrentEndResult == GameResult.EndResult.FiftyMoveRule)
                    return 0;
            }
            

            if (transpositionTable.GetEvaluation(board.CurrentGameState.ZobristHash) != LookUpFailed)
            {
                Entry entry = transpositionTable.LookUpZobrist(board.CurrentGameState.ZobristHash);

                if (entry.nodeType == LowerBound)
                    alpha = entry.score > alpha ? entry.score : alpha;

                else if (entry.nodeType == UpperBound)
                    beta = entry.score < beta ? entry.score : beta;

                else if (entry.depth >= ply)
                {
                    if (ply == CurrentSearchDepth)
                    {
                        BestMoveThisIteration = entry.bestMove;
                        BestEvalThisIteration = entry.score;
                    }
                    
                    return entry.score;
                }
            }

             // Full depth searched
            if (ply == 0)
            {
                return evaluator.Evaluate(board);
            }

            // Generate all moves
            Span<Move> spanMoves = MoveGenerator.GenerateAllMoves(board, false);

            // Sort moves
            Move[] moves = spanMoves.ToArray();
            Array.Sort(moves, new MVVLVASorter(board));

            int moveCount = moves.Length;

            // If no moves available, it is either stalemate or checkmate, so return score accordingly
            if (moveCount == 0)
            {
                if (board.CurrentEndResult == GameResult.EndResult.Stalemate)
                    return 0;

                return negativeInfinity;
            }

            int origAlpha = alpha;
            Move BestMoveInThisPos = Move.NullMove;

            // Start alpha-beta search
            foreach (Move move in moves)
            {
                board.MakeMove(move, inSearch: true);

                int eval = -Search(ply - 1, -beta, -alpha);

                board.UnMakeMove(move, inSearch: true);


                if (SearchTimer.ElapsedMilliseconds >= 3000)
                {
                    SearchCanceled = true;
                    return 0;
                }

                // Current position is the best, update alpha and set the best move in this position
                if (eval > alpha)
                {
                    BestMoveInThisPos = move;
                    alpha = eval;

                    // Best move at root, update the best move and best eval this iteration
                    if (ply == CurrentSearchDepth)
                    {
                        BestEvalThisIteration = eval;
                        BestMoveThisIteration = move; 
                    }
                }

                if (SearchCanceled)
                    return 0;

                if (alpha >= beta) 
                    break;
            }

            sbyte nodeType = SetNodeType(alpha, beta, origAlpha);
            transpositionTable.StoreEntry(new Entry(board.CurrentGameState.ZobristHash, BestMoveInThisPos, ply, alpha, nodeType));

            return alpha;
        }
    }
}
