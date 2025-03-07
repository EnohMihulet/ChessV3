using System;
using System.Collections.Generic;
using UnityEngine;

namespace Chess.Core
{
    public class PieceList
    {
        public int[] Pieces;
        public int[] Map;
        public int Count;

        public PieceList(int MaxPieceCount = 16)
        {
            Pieces = new int[MaxPieceCount];
            Map = new int[64];
            Count = 0;
        }


        public void AddPiece(int square)
        {
            Pieces[Count] = square;
            Map[square] = Count;
            Count++;
        }


        public void MovePiece(int startSquare, int targetSquare)
        {
            int index = Map[startSquare];
            Pieces[index] = targetSquare;
            Map[targetSquare] = index;
        }


        public void RemovePiece(int square)
        {
            int index = Map[square];
            Pieces[index] = Pieces[Count - 1];
            Map[Pieces[index]] = index;
            Count--;
        }
    }
}