using System;
using System.Collections.Generic;
using static Chess.Core.SQ;
using UnityEngine;
using Unity.Mathematics;
using System.Linq;

namespace Chess.Core
{    
    public class Evaluation
    {
        // Piece Values
        public static int[] PieceValues = {0, 1, 3, 3, 5, 9, 100};
        public static int[] WeightedPieceValues = {0, 100, 300, 300, 500, 900, 0};

        // Piece value adjustments
        static int[] KnightAdj = {-20, -16, -12, -8, -4,  0,  4,  8, 12};
        static int[] RookAdj = {15,  12,   9,  6,  3,  0, -3, -6, -9};

        // Piece bonuses
        const int KingShieldVal = 25;
        const int RookOpenFile = 20;
        const int RookHalfOpenFile = 10;

        // Piece penalties
        const int CentralPawnBlocked = -20;
        const int KingBlocksRook = -10;
        const int KnightTrapped = -100;
        const int BishopTrapped = -100;
        const int ReturningBishop = 0;
        const int EarlyQueenDev = -3; // Applied per undeveloped knight/bishop

        // Pair bonuses/maluses
        const int BishopPair = 40;
        const int KnightPair = -10;
        const int RookPair = 25;

        // Saftey table
        public static int[] SafetyTable = {
        0,  0,   1,   2,   3,   5,   7,   9,  12,  15,
        18,  22,  26,  30,  35,  39,  44,  50,  56,  62,
        68,  75,  82,  85,  89,  97, 105, 113, 122, 131,
        140, 150, 169, 180, 191, 202, 213, 225, 237, 248,
        260, 272, 283, 295, 307, 319, 330, 342, 354, 366,
        377, 389, 401, 412, 424, 436, 448, 459, 471, 483,
        494, 500, 500, 500, 500, 500, 500, 500, 500, 500,
        500, 500, 500, 500, 500, 500, 500, 500, 500, 500,
        500, 500, 500, 500, 500, 500, 500, 500, 500, 500,
        500, 500, 500, 500, 500, 500, 500, 500, 500, 500
        };
        
        // Game phase variables
        public byte totalPhase = 24;
        public byte gamePhase = 0;

        // Mid-game,end-game, and final scores
        public float mgScore;
        public float egScore;
        public float result;

        // Piece weights for phase calculation
        public byte phaseWeight;

        public int Evaluate(Board board) 
        {
            mgScore = 0;
            egScore = 0;
            gamePhase = 0;

            int color = board.ColorToMove;
            int enemyColor = color == 0 ? 1 : 0;

            // Sum material, add PCSQ values, and evaluate individual pieces
            for (int i = 1; i < 15; i++)
            {
                if (i == 7 || i == 8) // Are not piece values;
                    continue;

                int pieceColor = Piece.PieceColor(i);
                int sign = pieceColor == color ? 1 : -1;

                // Add piece square table values to mg and eg scores
                // Also evaluate individual pieces (not their weighted value)
                for (int j = 0; j <  board.AllPieces[i].Count; j++)
                {
                    int square = board.AllPieces[i].Pieces[j];

                    PieceSquareCalculations(Piece.PieceType(i), square, sign);

                    int pieceScore = EvalPiece(board, square, i) * sign;
                    mgScore += pieceScore;
                    egScore += pieceScore;
                }

                // Add weighted piece values to mg and eg scores
                mgScore += WeightedPieceValues[Piece.PieceType(i)] * board.AllPieces[i].Count * sign;
                egScore += WeightedPieceValues[Piece.PieceType(i)] * board.AllPieces[i].Count * sign;
            }

            // King shield 
            mgScore += KingShield(board, color) - KingShield(board, enemyColor);

            // Bishop and rook blockages
            mgScore += Blockages(board, color) - Blockages(board, enemyColor);

            // Pair bonuses
            int bishopPairBonus = board.AllPieces[Piece.MakePiece(Piece.Bishop, board.MoveColor)].Count > 1 ? BishopPair : 0;
            bishopPairBonus -= board.AllPieces[Piece.MakePiece(Piece.Bishop, board.OpponentColor)].Count > 1 ? BishopPair : 0;
            int knightPairBonus = board.AllPieces[Piece.MakePiece(Piece.Knight, board.MoveColor)].Count > 1 ? KnightPair : 0;
            knightPairBonus -= board.AllPieces[Piece.MakePiece(Piece.Knight, board.OpponentColor)].Count > 1 ? KnightPair : 0;
            int rookPairBonus = board.AllPieces[Piece.MakePiece(Piece.Rook, board.MoveColor)].Count > 1 ? RookPair : 0;
            rookPairBonus -=  board.AllPieces[Piece.MakePiece(Piece.Rook, board.OpponentColor)].Count > 1 ? RookPair : 0;

            mgScore += bishopPairBonus + knightPairBonus + rookPairBonus;
            egScore += bishopPairBonus + knightPairBonus + rookPairBonus;

            // TODO Pawn structure

            // Calculate final score based on game phase
            int mgWeight = gamePhase;
            int egWeight = totalPhase - gamePhase;
            return (int) ((mgScore * mgWeight + egScore * egWeight) / totalPhase);
        }
        

        private void PieceSquareCalculations(int pieceType, int square, int sign)
        {
            square = (sign == 1) ? square : PieceSquareTables.GetFlippedPosition(square);
            // PIECE SQUARE TABLES
            switch (pieceType) 
            {
                case 1:
                    mgScore += PieceSquareTables.mg_pawn_table[square] * sign;
                    egScore += PieceSquareTables.eg_pawn_table[square] * sign;
                    break;
                case 2:
                    mgScore += PieceSquareTables.mg_knight_table[square] * sign;
                    egScore += PieceSquareTables.eg_knight_table[square] * sign;
                    break;
                case 3:
                    mgScore += PieceSquareTables.mg_bishop_table[square] * sign;
                    egScore += PieceSquareTables.eg_bishop_table[square] * sign;
                    break;
                case 4:
                    mgScore += PieceSquareTables.mg_rook_table[square] * sign;
                    egScore += PieceSquareTables.eg_rook_table[square] * sign;
                    break;
                case 5:
                    mgScore += PieceSquareTables.mg_queen_table[square] * sign;
                    egScore += PieceSquareTables.eg_queen_table[square] * sign;
                    break;
                case 6:
                    mgScore += PieceSquareTables.mg_king_table[square] * sign;
                    egScore += PieceSquareTables.eg_king_table[square] * sign;
                    break;
            }
        }


        int KingShield(Board board, int color) {

            int result = 0; 
            int pawnOffset1 = color == 0 ? 0 : 40;
            int pawnOffset2 = color == 0 ? 0 : 24;
            
            // King is on kingside (or queenside) if it's col is greater than (or less than) the kingSideCol (or queenSideCol)
            int kingSideCol = 4;
            int queenSideCol = 3;

            int kingPos = board.Kings[color];
            int kingCol = kingPos % 8;
            int kingRank = kingPos / 8;

            int backRank = color == 0 ? 0 : 7;

            // King is not on the back rank
            if (kingRank != backRank)
            {
                return 0;
            }

            // Calculate kingside shield score
            if (kingCol > kingSideCol)
            {
                if (Piece.IsPiece(board, color, Piece.Pawn, f2 + pawnOffset1)) result += KingShieldVal;
                else if (Piece.IsPiece(board, color, Piece.Pawn, f3 + pawnOffset2)) result += KingShieldVal;

                if (Piece.IsPiece(board, color, Piece.Pawn, g2 + pawnOffset1)) result += KingShieldVal;
                else if (Piece.IsPiece(board, color, Piece.Pawn, g3 + pawnOffset2)) result += KingShieldVal;

                if (Piece.IsPiece(board, color, Piece.Pawn, h2 + pawnOffset1)) result += KingShieldVal;
                else if (Piece.IsPiece(board, color, Piece.Pawn, h3 + pawnOffset2)) result += KingShieldVal;
            }
            // Calculate queenside sheild score
            else if (kingCol < queenSideCol)
            {
                if (Piece.IsPiece(board, color, Piece.Pawn, a2 + pawnOffset1)) result += KingShieldVal;
                else if (Piece.IsPiece(board, color, Piece.Pawn, a3 + pawnOffset2)) result += KingShieldVal;

                if (Piece.IsPiece(board, color, Piece.Pawn, b2 + pawnOffset1)) result += KingShieldVal;
                else if (Piece.IsPiece(board, color, Piece.Pawn, b3 + pawnOffset2)) result += KingShieldVal;

                if (Piece.IsPiece(board, color, Piece.Pawn, c2 + pawnOffset1)) result += KingShieldVal;
                else if (Piece.IsPiece(board, color, Piece.Pawn, c3 + pawnOffset2)) result += KingShieldVal;
            }

            return result;
        }

        int Blockages(Board board, int color)
        {
            int result = 0;

            int offset = color == 0 ? 0 : 56;
            int pawnOffset = color == 0 ? 0 : 40;
            int blockedOffset = color == 0 ? 0 : 24;

            // central pawn blocked, bishop hard to develop
            if (Piece.IsPiece(board, 0, Piece.Bishop, b1 + offset) && Piece.IsPiece(board, 0, Piece.Pawn, d2 + pawnOffset) && board.Chessboard[d3 + blockedOffset] != 0)
                result += CentralPawnBlocked;
            if (Piece.IsPiece(board, 0, Piece.Bishop, f1 + offset) && Piece.IsPiece(board, 0, Piece.Pawn, e2 + pawnOffset) && board.Chessboard[e3 + blockedOffset] != 0)
                result += CentralPawnBlocked;

            // uncastled king blocking own rook
            if ((Piece.IsPiece(board, 0, Piece.King, e1 + offset) || Piece.IsPiece(board, 0, Piece.King, f1 + offset)) &&
                (Piece.IsPiece(board, 0, Piece.Rook, g1 + offset) || Piece.IsPiece(board, 0, Piece.Rook, h1 + offset)))
                result += KingBlocksRook;

            if ((Piece.IsPiece(board, 0, Piece.King, c1 + offset) || Piece.IsPiece(board, 0, Piece.King, d1 + offset)) &&
                (Piece.IsPiece(board, 0, Piece.Rook, a1 + offset) || Piece.IsPiece(board, 0, Piece.Rook, b1 + offset)))
                result += KingBlocksRook;

            return result;
        }


        int EvalPiece(Board board, int square, int piece)
        {
            int pieceType = Piece.PieceType(piece);
            int pieceColor = Piece.PieceColor(piece);

            switch (pieceType)
            {
                case 1:
                    break;
                case 2:
                    return EvalKnight(board, square, pieceColor);
                case 3:
                    return EvalBishop(board, square, pieceColor);
                case 4:
                    return EvalRook(board, square, pieceColor);
                case 5:
                    return EvalQueen(board, square, pieceColor);
            }

            return 0;
        }


        int EvalPawnStructure()
        {
            return 0;
        }


        int EvalKnight(Board board,int square, int color)
        {
            int mobility = 0;
            int result = 0;
            gamePhase += 1;

            // Adjust knight score based on pawn count
            int ownPawnCount = board.AllPieces[1 + color == 0 ? 0 : 8].Count;
            result += KnightAdj[ownPawnCount];
            
            // Apply penalty for trapped knight
            if (color == 0)
            {
                switch (square)
                {
                    case a8: if (Piece.IsPiece(board, 1, Piece.Pawn, a7) || Piece.IsPiece(board, 1, Piece.Pawn, c7)) result += KnightTrapped; break;
                    case h8: if (Piece.IsPiece(board, 1, Piece.Pawn, h7) || Piece.IsPiece(board, 1, Piece.Pawn, f7)) result += KnightTrapped; break;
                    case a7: if (Piece.IsPiece(board, 1, Piece.Pawn, a6) || Piece.IsPiece(board, 1, Piece.Pawn, b7)) result += KnightTrapped; break;
                    case h7: if (Piece.IsPiece(board, 1, Piece.Pawn, h6) || Piece.IsPiece(board, 1, Piece.Pawn, g7)) result += KnightTrapped; break;
                    case c3: if (Piece.IsPiece(board, 0, Piece.Pawn, c2) || Piece.IsPiece(board, 0, Piece.Pawn, d4) && !Piece.IsPiece(board, 0, Piece.Pawn, e4)) result += KnightTrapped; break;
                }
            }
            else
            {
                switch (square)
                {
                    case a1: if (Piece.IsPiece(board, 0, Piece.Pawn, a2) || Piece.IsPiece(board, 0, Piece.Pawn, c2)) result += KnightTrapped; break;
                    case h1: if (Piece.IsPiece(board, 0, Piece.Pawn, h2) || Piece.IsPiece(board, 0, Piece.Pawn, f2)) result += KnightTrapped; break;
                    case a2: if (Piece.IsPiece(board, 0, Piece.Pawn, a3) || Piece.IsPiece(board, 0, Piece.Pawn, b2)) result += KnightTrapped; break;
                    case h2: if (Piece.IsPiece(board, 0, Piece.Pawn, h3) || Piece.IsPiece(board, 0, Piece.Pawn, g2)) result += KnightTrapped; break;
                    case c6: if (Piece.IsPiece(board, 1, Piece.Pawn, c7) || Piece.IsPiece(board, 1, Piece.Pawn, d5) && !Piece.IsPiece(board, 1, Piece.Pawn, e5)) result += KnightTrapped; break;
                }
            }

            // Calculate Mobility score
            int directionCount = BoardHelper.KnightCoorDirections.Length;

            // Loop through each possible knight move offset
            for (int index = 0; index < directionCount; index++)
            {  
                int targetRank = square / 8 + BoardHelper.KnightCoorDirections[index].rank;
                int targetFile = square % 8 + BoardHelper.KnightCoorDirections[index].file;

                Coordinate targetCoor = new Coordinate(targetRank, targetFile);
                int targetSquare = targetRank * 8 + targetFile;
                
                // Move is inbounds
                if (!targetCoor.IsInBounds())
                    continue; 

                int movePiece = board.Chessboard[square];
                int movePieceColor = Piece.PieceColor(movePiece);

                int targetPiece = board.Chessboard[targetSquare];
                int targetPieceColor = Piece.PieceColor(targetPiece);

                // Add move
                if (movePieceColor != targetPieceColor || targetPiece == Piece.None)
                    mobility++;
            }

            return result;
        }


        int EvalBishop(Board board, int square, int color)
        {
            int mobility = 0;
            int result = 0;
            gamePhase += 1;

            if (color == 0)
            {
                switch (square)
                {
                    case a7: if (Piece.IsPiece(board, 1, Piece.Pawn, b6)) result += BishopTrapped; break;
                    case h7: if (Piece.IsPiece(board, 1, Piece.Pawn, g6)) result += BishopTrapped; break;
                    case b8: if (Piece.IsPiece(board, 1, Piece.Pawn, c7)) result += BishopTrapped; break;
                    case g8: if (Piece.IsPiece(board, 1, Piece.Pawn, f7)) result += BishopTrapped; break;
                    case a6: if (Piece.IsPiece(board, 1, Piece.Pawn, b5)) result += BishopTrapped; break;
                    case h6: if (Piece.IsPiece(board, 1, Piece.Pawn, g5)) result += BishopTrapped; break;
                    case f1: if (Piece.IsPiece(board, 0, Piece.King, g1)) result += ReturningBishop; break;
                    case c1: if (Piece.IsPiece(board, 0, Piece.King, b1)) result += ReturningBishop; break;
                }
            }
            else
            {
                switch (square)
                {
                    case a2: if (Piece.IsPiece(board, 0, Piece.Pawn, b3)) result += BishopTrapped; break;
                    case h2: if (Piece.IsPiece(board, 0, Piece.Pawn, g3)) result += BishopTrapped; break;
                    case b1: if (Piece.IsPiece(board, 0, Piece.Pawn, c2)) result += BishopTrapped; break;
                    case g1: if (Piece.IsPiece(board, 0, Piece.Pawn, f2)) result += BishopTrapped; break;
                    case a3: if (Piece.IsPiece(board, 0, Piece.Pawn, b4)) result += BishopTrapped; break;
                    case h3: if (Piece.IsPiece(board, 0, Piece.Pawn, g4)) result += BishopTrapped; break;
                    case f8: if (Piece.IsPiece(board, 1, Piece.King, g8)) result += ReturningBishop; break;
                    case c8: if (Piece.IsPiece(board, 1, Piece.King, b8)) result += ReturningBishop; break;
                }
            }

            int directionCount = BoardHelper.DiagonalSquareDirections.Length;

            // Follow each diagonal until target square is out of bounds, an enemy piece (capture), or ally piece
            for (int index = 0; index < directionCount; index++)
            {
                int direction = BoardHelper.DiagonalSquareDirections[index];

                int startSquare = square;
                int targetSquare = square + direction;
                int piece = board.Chessboard[startSquare];

                while (BoardHelper.SquareInBounds(targetSquare))
                {
                    // Stops bishop from wrapping around the board
                    if (math.abs(targetSquare / 8 - startSquare / 8) != math.abs(targetSquare % 8 - startSquare % 8))
                        break;

                    int targetPiece = board.Chessboard[targetSquare];

                    // Capture
                    if (Piece.PiecesAreDifferentColor(piece, targetPiece))
                    {
                        mobility++;
                        break;
                    }

                    // Non-capture move
                    if (Piece.IsNone(targetPiece))
                        mobility++;

                    // Ally Piece
                    else if (Piece.PiecesAreSameColor(piece, targetPiece)) 
                        break;

                    targetSquare += direction;
                }
            }

            result += 3 * (mobility - 7);
        
            return result;
        }


        int EvalRook(Board board, int square, int color)
        {
            int result = 0;
            int mobility = 0;
            bool openFile = true;
            bool halfOpenFile = true;
            gamePhase += 2;

            // Adjust rook score based on pawn count
            int ownPawnCount = board.AllPieces[1 + color == 0 ? 0 : 8].Count;
            result += RookAdj[ownPawnCount];

            // Detect open/half open files and score mobility
            int directionCount = BoardHelper.StraightSquareDirections.Length;

            // Follow each rank/row until target square is out of bounds, an enemy piece (capture), or ally piece
            for (int index = 0; index < directionCount; index++)
            {
                int direction = BoardHelper.StraightSquareDirections[index];

                int startSquare = square;
                int targetSquare = square + direction;
                int piece = board.Chessboard[startSquare];

                while (BoardHelper.SquareInBounds(targetSquare))
                {   
                    // Stops rook from wrapping around the board
                    if (targetSquare / 8 != startSquare / 8 && math.abs(direction) == 1)
                        break;
                    if (targetSquare % 8 != startSquare % 8 && math.abs(direction) == 8)
                        break;

                    int targetPiece = board.Chessboard[targetSquare];

                    // Capture
                    if (Piece.PiecesAreDifferentColor(piece, targetPiece))
                    {
                        mobility++;
                        if (direction != 8)
                            break;
                    }

                    // Non-capture move
                    if (Piece.IsNone(targetPiece))
                    {
                        mobility++;
                        if (direction != 8)
                            break;
                    }

                    // // Ally Piece
                    if (direction != 8 && Piece.PiecesAreSameColor(piece, targetPiece))
                        break;

                    // Detect open/half open files
                    if (direction == 8)
                    {
                        int targetColor = Piece.PieceColor(targetPiece);
                        if ((targetPiece == 1 || targetPiece == 9) && targetColor == color)
                        {
                            openFile = false;
                        }
                        else if ((targetPiece == 1 || targetPiece == 9) && targetColor != color)
                        {
                            if (openFile == false)
                                halfOpenFile = false;
                            else
                                openFile = false;
                        }
                    }

                    targetSquare += direction;
                }
            }

            result += halfOpenFile ? RookHalfOpenFile : 0;
            result += openFile ? RookOpenFile : 0;

            // Should return 2 * (mob - 7) for mg and 4 * (mob - 7)
            result += 3 * (mobility - 7);
            // result += 2 * (mobility - 7);
            // result += 4 * (mobility - 7);

            return result;
        }


        int EvalQueen(Board board, int square, int color)
        {
            int result = 0;
            int mobility = 0;
            gamePhase += 4;

            // Apply penalty for early queen development
            // White queen is past the second rank
            if (color == 0 && square / 8 > 1)
            {
                // Knights/bishops are undeveloped
                if (Piece.IsPiece(board, 0, Piece.Knight, b1)) result += EarlyQueenDev;
                if (Piece.IsPiece(board, 0, Piece.Bishop, c1)) result += EarlyQueenDev;
                if (Piece.IsPiece(board, 0, Piece.Bishop, f1)) result += EarlyQueenDev;
                if (Piece.IsPiece(board, 0, Piece.Knight, g1)) result += EarlyQueenDev;
            }
            else if (color == 1 && square / 8 < 6)
            {
                // Knights/bishops are undeveloped
                if (Piece.IsPiece(board, 1, Piece.Knight, b8)) result += EarlyQueenDev;
                if (Piece.IsPiece(board, 1, Piece.Bishop, c8)) result += EarlyQueenDev;
                if (Piece.IsPiece(board, 1, Piece.Bishop, f8)) result += EarlyQueenDev;
                if (Piece.IsPiece(board, 1, Piece.Knight, g8)) result += EarlyQueenDev;
            }

            int[] queenSquareDirections = BoardHelper.StraightSquareDirections.Concat(BoardHelper.DiagonalSquareDirections).ToArray();
            int directionCount = queenSquareDirections.Length;
            // Follow each rank/row until target square is out of bounds, an enemy piece (capture), or ally piece
            for (int index = 0; index < directionCount; index++)
            {
                int direction = queenSquareDirections[index];

                int startSquare = square;
                int targetSquare = square + direction;
                int piece = board.Chessboard[startSquare];

                while (BoardHelper.SquareInBounds(targetSquare))
                {   
                    // Stops diagonal moves from wrapping around the board
                    if (math.abs(direction) == 7 || math.abs(direction) == 9)
                    {
                        if (math.abs(targetSquare / 8 - startSquare / 8) != math.abs(targetSquare % 8 - startSquare % 8))
                            break;
                    }
                    // Stops horizontal/vertical from wrapping around the board
                    else 
                    {
                        if (targetSquare / 8 != startSquare / 8 && math.abs(direction) == 1)
                            break;
                        if (targetSquare % 8 != startSquare % 8 && math.abs(direction) == 8)
                            break;
                    }
                        
                    int targetPiece = board.Chessboard[targetSquare];

                    // Capture
                    if (Piece.PiecesAreDifferentColor(piece, targetPiece))
                    {
                        mobility++;
                        break;
                    }
                    // Non-capture move
                    if (Piece.IsNone(targetPiece))
                    {
                        mobility++;
                    }
                    // Ally Piece
                    else if (Piece.PiecesAreSameColor(piece, targetPiece))
                        break;

                    targetSquare += direction;
                }
            }

            result += 1 * (mobility - 14);
            // result += 2 * (mobility - 14);

            return result;
        }
    }
}