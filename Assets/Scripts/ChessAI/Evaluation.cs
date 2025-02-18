using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace Chess.Core
{    
    public class Evaluation
    {
        public static int[] PieceValues = {0, 1, 3, 3, 5, 9, 100};
        public static int[] WeightedPieceValues = {0, 100, 300, 300, 500, 900, 0};

        public static int Evaluate(Board board) 
        {
            // Define constants to gauge the game phase
            byte totalPhase = 24;
            byte whitePhase = 0;
            byte blackPhase = 0;

            // Accumulate separate mid-game and end-game scores and then blend them
            float mgScore = 0;
            float egScore = 0;

            // Count material and also build the mg/eg scores
            for (byte i = 0; i < 64; i++) {
                int piece = board.Chessboard[i];
                int pieceType = Piece.PieceType(piece);
                int pieceColor = Piece.PieceColor(piece);

                if (pieceType == 0) 
                    continue;

                // Determine sign based on who is board.ColorToMove
                sbyte sign = (sbyte) ((pieceColor == board.CurrentGameState.ColorToMove) ? 1 : -1);
                // Base piece value
                float baseVal = (float)(WeightedPieceValues[pieceType] * 1.5);
                // Determine indexing for piece-square tables
                byte square = (byte)((pieceColor == 1) ? i : 63 - i);

                // Define piece weights for phase calculation
                byte phaseWeight = 0;
                switch (pieceType) 
                {
                    case 2: phaseWeight = 1; break;
                    case 3: phaseWeight = 1; break;
                    case 4: phaseWeight = 2; break;
                    case 5: phaseWeight = 4; break;
                }

                if (pieceColor == 1) 
                    whitePhase += phaseWeight;
                else 
                    blackPhase += phaseWeight;

                float mgVal = 0;
                float egVal = 0;

                // PIECE SQUARE TABLES
                switch (pieceType) 
                {
                    case 1:
                        mgVal = PieceSquareTables.mg_pawn_table[square];
                        egVal = PieceSquareTables.eg_pawn_table[square];
                        break;
                    case 2:
                        mgVal = PieceSquareTables.mg_knight_table[square];
                        egVal = PieceSquareTables.eg_knight_table[square];
                        break;
                    case 3:
                        mgVal = PieceSquareTables.mg_bishop_table[square];
                        egVal = PieceSquareTables.eg_bishop_table[square];
                        break;
                    case 4:
                        mgVal = PieceSquareTables.mg_rook_table[square];
                        egVal = PieceSquareTables.eg_rook_table[square];
                        break;
                    case 5:
                        mgVal = PieceSquareTables.mg_queen_table[square];
                        egVal = PieceSquareTables.eg_queen_table[square];
                        break;
                    case 6:
                        mgVal = PieceSquareTables.mg_king_table[square];
                        egVal = PieceSquareTables.eg_king_table[square];
                        break;
                }

                // Accumulate mg and eg scores
                mgScore += (baseVal + mgVal) * sign;
                egScore += (baseVal + egVal) * sign;
            }
            
            // Calculate the current phase
            // The remaining phase: The more material off the board, the closer to the endgame.
            int remainingPhase = whitePhase + blackPhase; 
            if (remainingPhase > totalPhase) remainingPhase = totalPhase; // just a sanity check

            // phaseFactor = remainingPhase / totalPhase: 
            // = 1.0 at start (all material present), linearly towards 0.0 in the endgame.
            float phaseFactor = (float)remainingPhase / totalPhase;

            float finalScore = mgScore * phaseFactor + egScore * (1.0f - phaseFactor);

            return (int) finalScore;
        }
    }
}