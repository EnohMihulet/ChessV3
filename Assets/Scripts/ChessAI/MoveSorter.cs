using System;
using Unity.Mathematics;
using System.Collections.Generic;
using UnityEngine.UIElements;
using Unity.VisualScripting;

namespace Chess.Core
{
    public class MoveSorter
    {
        int[] scores;
        int MaxMoveCount = 218;
        int KillerMoveMaxPly = 32;

        static int[] pieceValues = {0, 100, 300, 320, 500, 900, 20000};
        
        public const int PVMoveBonus = 10000; // Principal Variation move
        public const int KillerMoveBonus = 9000; // Killer moves
        public const int CountMoveBonus = 8500; // Counter moves
        // Capture moves scoring using a MVV/LVA approach
        public const int BaseCaptureBonus = 1000;   // Calculated as: (victimValue - attackerValue) + BaseCaptureBonus
        public const int PromotionBonus = 8000; // Promotion bonus
        public const int CheckBonus = 50; // Check Bonus
        public const int QuietMoveBonus = 0; // Quiet moves
        public const int MaxHistoryBonus = 1000; // Max bonus from the history heuristic

        public KillerMove[] killerMoves;
        public int[,,] historyTable; // Indexed by: color to move, from square, to square
        public Move[,] counterTable; // Indexed by: color to move, from square, to square

        public MoveSorter()
        {
            scores = new int[MaxMoveCount];
            killerMoves = new KillerMove[KillerMoveMaxPly];
            historyTable = new int[2,64,64];
            counterTable = new Move[64, 64];
        }


        public struct KillerMove
        {
            Move move1;
            Move move2;

            public void Add(Move move)
            {
                if (move != move1)
                {
                    move2 = move1;
                    move1 = move;
                }
            }

            public bool IsKillerMove(Move move)
            {
                if (move == move1 || move == move2)
                    return true;
                return false;
            }
        }


        public void Sort(Span<Move> moves, Board board, Move prevBestMove, int currentPly)
        {
            ScoreMoves(moves, board, prevBestMove, currentPly);
            
            QuickSort(moves, scores, 0, moves.Length - 1);
        }


        void ScoreMoves(Span<Move> moves, Board board, Move prevBestMove, int pliesFromRoot)
        {
            int moveCount = moves.Length;

            for (int i = 0; i < moveCount; i++)
            {
                if (moves[i] == prevBestMove)
                {
                    scores[i] = PVMoveBonus;
                    continue;
                }

                if (moves[i].IsCapture(board))
                {
                    int pieceType = Piece.PieceType(board.Chessboard[moves[i].StartSquare]);
                    int capturedPieceType = Piece.PieceType(board.Chessboard[moves[i].TargetSquare]);

                    scores[i] = pieceValues[capturedPieceType] - pieceValues[pieceType] + BaseCaptureBonus;
                }
                else
                {
                    scores[i] += QuietMoveBonus;
                    
                    if (pliesFromRoot >= KillerMoveMaxPly)
                        continue;

                    if (killerMoves[pliesFromRoot].IsKillerMove(moves[i]))
                        scores[i] += KillerMoveBonus;

                    // Also include killer moves from 2 plies ago
                    if (pliesFromRoot >= 2 && killerMoves[pliesFromRoot - 2].IsKillerMove(moves[i]))
                    {
                        scores[i] += KillerMoveBonus;
                    }

                    int movesMade = board.AllGameMoves.Count;
                    if (movesMade > 0 && moves[i] == counterTable[board.AllGameMoves[movesMade - 1].StartSquare, board.AllGameMoves[movesMade - 1].TargetSquare])
                        scores[i] += CountMoveBonus;
                    
                    scores[i] += historyTable[board.ColorToMove, moves[i].StartSquare, moves[i].TargetSquare];
                }


                if (moves[i].IsPromotion)
                {
                    scores[i] += PromotionBonus;
                }
            }
        }


        static void QuickSort(Span<Move> moves, int[] scores, int low, int high)
        {
            if (low < high)
            {
                int pivotIndex = Partition(moves, scores, low, high);
                QuickSort(moves, scores, low, pivotIndex - 1);
                QuickSort(moves, scores, pivotIndex + 1, high);
            }
        }


        static int Partition(Span<Move> moves, int[] scores, int low, int high)
        {
            int pivotScore = scores[high];
            int i = low - 1;

            for (int j = low; j <= high - 1; j++)
            {
                if (scores[j] > pivotScore)
                {
                    i++;
                    Swap(moves, scores, i, j);
                }
            }

            Swap(moves, scores, i + 1, high);
            return i + 1;
        }


        static void Swap(Span<Move> moves, int[] scores, int i, int j)
        {
            Move tempMove = moves[i];
            int tempScore = scores[i];

            moves[i] = moves[j];
            scores[i] = scores[j];
            moves[j] = tempMove;
            scores[j] = tempScore;
        }   
    }
}