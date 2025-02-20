using System;
using System.Collections.Generic;
using Chess.Core;
using UnityEngine;

namespace Chess
{
    class TranspositionTable
    {
        public const int LookUpFailed = -1;
        public const sbyte Exact = 0; // The stored eval is the exact score for that position
        public const sbyte LowerBound = -1; // The true score is at least this value
        public const sbyte UpperBound = 1; // The true score of at most this value
        public const int depthCutOff = 4;
        const int entriesSize = 524288; // 2^19
        public Entry[] entries;

        public TranspositionTable()
        {
            entries = new Entry[entriesSize];
        }
        
        public struct Entry
        {
            public readonly ulong zobrist;
            public readonly int score;
            public readonly Move bestMove;
            public readonly byte depth;
            public readonly sbyte nodeType;

            public Entry(ulong zobrist, Move bestMove, int depth, int score, int nodeType)
            {
                this.zobrist = zobrist;
                this.score = score;
                this.bestMove = bestMove;
                this.depth = (byte) depth;
                this.nodeType = (sbyte) nodeType;
            }

            public static Entry Empty => new Entry(0, Move.NullMove, 0, 0, 0);
        }

        public void ClearEntries()
        {
            for (int i = 0; i < entriesSize; i++)
            {
                entries[i] = Entry.Empty;
            }
        }   


        public int GetIndex(ulong zobrist)
        {
            return (int) (zobrist & (ulong)(entriesSize - 1));
        }


        public void StoreEntry(Entry entry)
        {
            int index = GetIndex(entry.zobrist);

            if (entries[index].bestMove != Move.NullMove)
            {
                // Handle collision
                int oldDepth = entries[index].depth;
                int newDepth = entry.depth;

                // If old entry searched deeper, keep it.
                if (oldDepth > newDepth)
                    return;
            }
            
            // Store new entry at index
            entries[index] = entry;
        }


        public int GetEvaluation(ulong zobrist)
        {
            int index = GetIndex(zobrist);

            Entry entry = entries[index];

            if (zobrist == entry.zobrist)
            {
                return entry.score;
            }
            else 
                return -1;
        }


        public Entry LookUpZobrist(ulong zobrist)
        {
            return entries[GetIndex(zobrist)];
        }


        public static sbyte SetNodeType(int alpha, int beta, int originalAlpha)
        {
            if (alpha == originalAlpha)
                return UpperBound;
            else if (alpha > beta)
                return LowerBound;

            return Exact;
        }
    }
}