using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Chess.Core
{
    /*
    Represents either a positon (rank, file) on the Chessboard
    or
    Represents an offset (1, 1) up 1 rank and right 1 rank
    */
    
    public class Coordinate
    {
        public readonly int file;
        public readonly int rank;

        public Coordinate(int rank, int file)
        {
            this.rank = rank;
            this.file = file;
        }
        
        public Coordinate(int square)
        {
            this.rank = BoardHelper.RankIndex(square);
            this.file = BoardHelper.FileIndex(square);
        }

        public int SquareIndex()
        {
            return rank * 8 + file;
        }

        public bool IsInBounds()
        {
            return 7 >= rank && rank >= 0 && 7 >= file && file >= 0;
        }

        public bool PieceIsColorToMove(Board board)
        {
            int pieceColor = Piece.PieceColor(board.Chessboard[this.SquareIndex()]);
            return pieceColor == board.CurrentGameState.ColorToMove ? true : false;
        } 

        public int CoorColor(Board board)
        {   
            return Piece.PieceColor(board.Chessboard[this.rank * 8 + this.file]);
        }
    }
}
