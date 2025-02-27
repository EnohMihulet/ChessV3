using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace Chess.Core
{    
    public class OldEvaluation
    {
        // Piece Values
        public static int[] PieceValues = {0, 1, 3, 3, 5, 9, 100};
        public static int[] WeightedPieceValues = {0, 100, 300, 300, 500, 900, 0};

        // Game phase constants
        public byte totalPhase = 24;
        public byte whitePhase;
        public byte blackPhase;

        // Mid-game / end-game scores
        public float mgScore;
        public float egScore;

        // Piece weights for phase calculation
        public byte phaseWeight;

        public int Evaluate(Board board) 
        {
            mgScore = 0;
            egScore = 0;
            whitePhase = 0;
            blackPhase = 0;

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
                float baseVal = WeightedPieceValues[pieceType];

                // Determine indexing for piece-square tables
                byte square = (byte)((pieceColor == 0) ? i : PieceSquareTables.GetFlippedPosition(i));

                PhaseCalculations(pieceType, pieceColor);

                PieceSquareCalculations(pieceType, square, baseVal, sign);
            }
            
            // The remaining phase: The more material off the board, the closer to the endgame.
            int remainingPhase = whitePhase + blackPhase; 

            // = 1.0 at start (all material present), linearly towards 0.0 in the endgame.
            float phaseFactor = (float)remainingPhase / totalPhase;

            return (int)(mgScore * phaseFactor + egScore * (1.0f - phaseFactor));
        }


        private void PhaseCalculations(int pieceType, int pieceColor)
        {
            switch (pieceType) 
            {
                case 2: phaseWeight = 1; break;
                case 3: phaseWeight = 1; break;
                case 4: phaseWeight = 2; break;
                case 5: phaseWeight = 4; break;
            }

            if (pieceColor == 0) 
                whitePhase += phaseWeight;
            else 
                blackPhase += phaseWeight;
        }

        
        private void PieceSquareCalculations(int pieceType, int square, float baseVal, int sign)
        {
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
    }
}