using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using static Chess.TranspositionTable;

namespace Chess.Core
{
    public class OldSearcher
    {

        const int positiveInfinity = 9999999;
		const int negativeInfinity = -positiveInfinity;
        public const int MaxEntensions = 0;
        public int CurrentSearchDepth;
        public Move BestMoveThisIteration;
        public int BestEvalThisIteration;

        public bool SearchCanceled = false;
        Stopwatch SearchTimer;
        const int TimePerMove = 1000; // 1 Second/s per move

        private bool inOpening = true;
        private string openingsFileName = "./Assets/Resources/Book.txt";

        public Move BestMove;
        public int BestEval;
        public Board board;
        private readonly TranspositionTable transpositionTable;
        private Evaluation evaluator;
        private MoveSorter moveSorter;
        private OpeningBook openingBook;

        public OldSearcher()
        {
            this.transpositionTable = new TranspositionTable();
            this.evaluator = new Evaluation();
            this.moveSorter = new MoveSorter();
            this.openingBook = new OpeningBook(openingsFileName);
        }

        public Move StartSearch(Board board)
        {   
            this.board = board;
            CurrentSearchDepth = 1;
            SearchCanceled = false;
            SearchTimer = Stopwatch.StartNew();

            BestMove = BestMoveThisIteration = Move.NullMove;
            BestEval = BestEvalThisIteration = negativeInfinity;

            if (inOpening)
            {   
                string StrMove;
                bool moveFound = openingBook.TryGetMove(board, out StrMove);
                if (moveFound)
                    return MoveHelper.StrMoveToMove(board, StrMove);
                else
                    inOpening = false;
            }

            IterativeDeepeningSearch();
            
            UnityEngine.Debug.Log("Old Depth: " + CurrentSearchDepth);
            return BestMove;
        }

        void IterativeDeepeningSearch()
        {
            for (int depth = 1; depth < 100; depth++)
            {
                CurrentSearchDepth = depth;

                Search(depth, 0, negativeInfinity, positiveInfinity);

                if (!SearchCanceled)
                {
                    BestEval = BestEvalThisIteration;
                    BestMove = BestMoveThisIteration;
                }

                if (SearchTimer.ElapsedMilliseconds >= TimePerMove)
                {
                    SearchCanceled = true;
                    return;
                }
            }
        }

        int Search(int pliesRemaining, int pliesFromRoot, int alpha, int beta)
        {
            // Searched cancelled, exit search
            if (SearchCanceled)
            {
                return 0;
            }

            if (CurrentSearchDepth > 0) {
                // Draw by repetition, FiftyMoveRule, or insufficient material return score for draw (0)
                if (board.CurrentEndResult == GameResult.EndResult.Repetition || board.CurrentEndResult == GameResult.EndResult.FiftyMoveRule ||
                    board.CurrentEndResult == GameResult.EndResult.InsufficientMaterial)
                    return 0;
            }
            
            if (transpositionTable.GetEvaluation(board.CurrentGameState.ZobristHash) != LookUpFailed)
            {
                Entry entry = transpositionTable.LookUpZobrist(board.CurrentGameState.ZobristHash);

                if (entry.nodeType == LowerBound)
                    alpha = entry.score > alpha ? entry.score : alpha;

                else if (entry.nodeType == UpperBound)
                    beta = entry.score < beta ? entry.score : beta;

                else if (entry.depth >= pliesRemaining)
                {
                    if (pliesFromRoot == 0)
                    {
                        BestMoveThisIteration = entry.bestMove;
                        BestEvalThisIteration = entry.score;
                    }
                    
                    return entry.score;
                }
            }

             // Full depth searched
            if (pliesRemaining == 0)
            {
                return evaluator.Evaluate(board);
            }

            // Generate all moves
            Span<Move> moves = MoveGenerator.GenerateAllMoves(board, false);

            // Sort moves
            Move prevBestMove = pliesFromRoot == 0 ? BestMove : transpositionTable.LookUpZobrist(board.CurrentGameState.ZobristHash).bestMove;
            moveSorter.Sort(moves, board, prevBestMove, pliesFromRoot);

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
            for (int i = 0; i < moveCount; i++)
            {
                Move move = moves[i];
                bool isCapture = move.IsCapture(board);
                int eval = 0;

                board.MakeMove(move, inSearch: true);

                bool reducedDepthSearch = pliesRemaining >= 3 && i >= 3 && !isCapture;
                bool fullSearch = true;
                if (reducedDepthSearch)
                {   
                    // Search 1 ply less 
                    eval = -Search(pliesRemaining - 2, pliesFromRoot + 1, -beta, -alpha);
                    // Move fails high, should research to full depth
                    fullSearch = eval > alpha;
                }
                if (fullSearch)
                {   
                    // Research to full depth
                    eval = -Search(pliesRemaining - 1, pliesFromRoot + 1, -beta, -alpha);
                }

                board.UnMakeMove(move, inSearch: true);

                if (SearchTimer.ElapsedMilliseconds >= TimePerMove)
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
                    if (pliesFromRoot == 0)
                    {
                        BestEvalThisIteration = eval;
                        BestMoveThisIteration = move; 
                    }
                }
                // Apply a penalty to quiet moves that do no perform well
                else if (!isCapture)
                {
                    int malus = -(300 * pliesFromRoot - 250);
                    moveSorter.historyTable[board.ColorToMove, move.StartSquare, move.TargetSquare] += HistoryBonus(board.ColorToMove, move, malus);
                }


                // Move is too good, opponent could avoid
                if (alpha >= beta)
                {

                    if (!isCapture)
                    {
                        // Prioritize this move in future searches
                        moveSorter.historyTable[board.ColorToMove, move.StartSquare, move.TargetSquare] += HistoryBonus(board.ColorToMove, move, pliesFromRoot * pliesFromRoot);
                        moveSorter.killerMoves[pliesFromRoot].Add(move);

                        // Apply a bonus to the move 
                        int movesMade = board.AllGameMoves.Count;
                        moveSorter.counterTable[board.AllGameMoves[movesMade - 1].StartSquare, board.AllGameMoves[movesMade - 1].TargetSquare] = move;
                    }

                    break;
                }


                if (SearchCanceled)
                    return 0;

            }

            sbyte nodeType = SetNodeType(alpha, beta, origAlpha);
            transpositionTable.StoreEntry(new Entry(board.CurrentGameState.ZobristHash, BestMoveInThisPos, pliesRemaining, alpha, nodeType));

            return alpha;
        }

        int HistoryBonus(int colorToMove, Move move, int bonus)
        {
            int clampedBonus = math.clamp(bonus, -MoveSorter.MaxHistoryBonus, MoveSorter.MaxHistoryBonus);

            int currentHistory = moveSorter.historyTable[colorToMove, move.StartSquare, move.TargetSquare];
            int delta = clampedBonus - currentHistory * math.abs(clampedBonus) / MoveSorter.MaxHistoryBonus;

            return delta;
        }
    }
}
