using System;
using System.Collections.Generic;
using System.IO;
using Unity.Mathematics;
using UnityEngine;

namespace Chess.Core
{
    public class OpeningBook
    {
        private Dictionary<string, BookPos> bookPositions;

        public OpeningBook(string fileName)
        {
            bookPositions = new Dictionary<string, BookPos>();
            LoadBook(fileName);
        }


        public struct BookMove
        {
            public string move { get; set; }
            public int frequency { get; set; }

            public BookMove(string move, int frequency)
            {
                this.move = move;
                this.frequency = frequency;
            }
        }


        public struct BookPos
        {
            public string fen { get; private set; }
            public List<BookMove> bookMoves { get; private set; }

            public BookPos(string fen)
            {
                this.fen = fen;
                bookMoves = new List<BookMove>();
            }

            public bool IsNull()
            {
                return fen == null;
            }
        }

        public void LoadBook(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Book file not found", filePath);

            var lines = File.ReadAllLines(filePath);
            BookPos bookPos = new BookPos();

            foreach (var rawLine in lines)
            {
                string line = rawLine.Trim();

                if (string.IsNullOrEmpty(line))
                    continue;

                if (line.StartsWith("pos"))
                {
                    string fen = line.Substring(4).Trim();
                    bookPos = new BookPos(fen);
                    bookPositions[fen] = bookPos;
                }
                else if (!bookPos.IsNull())
                {
                    var parts = line.Split(" ");
                    if (parts.Length >= 2 && int.TryParse(parts[1], out int freq))
                    {
                        string move = parts[0];
                        bookPos.bookMoves.Add(new BookMove(move, freq));
                    }
                    else
                        Debug.Log("Invalid move line: " + line);
                }
            }
        }


        List<BookMove> GetMovesFromFEN(string fen)
        {
            if (bookPositions.TryGetValue(fen, out BookPos position))
                return position.bookMoves;
            else
                return new List<BookMove>();
        }


        // Weight = 1 means a random move will be picked
        // Weight = 0 means the most played move will be picked
        public bool TryGetMove(Board board, out string move, float weight = 0.5f)
        {
            string fen = FenUtility.CurrentFen(board, alwaysIncludeEPSquare: false);
            fen = fen.Substring(0, fen.LastIndexOf(' '));
            fen = fen.Substring(0, fen.LastIndexOf(' '));
            var bookMoves = GetMovesFromFEN(fen);

            // Check if there are any moves
            if (bookMoves == null || bookMoves.Count == 0)
            {
                move = null;
                return false;
            }

            // Clamp weight between 0 and 1.
            weight = Mathf.Clamp(weight, 0f, 1f);

            // If weight == 0, return the most frequent move.
            if (weight == 0f)
            {
                move = bookMoves[0].move;
                return true;
            }
            // If weight == 1, pick uniformly at random.
            else if (weight == 1f)
            {
                int rngIndex = UnityEngine.Random.Range(0, bookMoves.Count);
                move = bookMoves[rngIndex].move;
                return true;
            }

            // Compute beta to interpolate between greedy and uniform.
            float beta = 1f / weight - 1f;

            // Compute weighted frequencies.
            float totalWeighted = 0f;
            List<float> weightedFrequencies = new List<float>();
            foreach (BookMove bookMove in bookMoves)
            {
                float weightedValue = Mathf.Pow(bookMove.frequency, beta);
                weightedFrequencies.Add(weightedValue);
                totalWeighted += weightedValue;
            }

            // Now, sample a move using the weighted probabilities.
            float randomValue = UnityEngine.Random.value; // Random float in [0,1)
            float cumulativeProbability = 0f;
            for (int i = 0; i < bookMoves.Count; i++)
            {
                float probability = weightedFrequencies[i] / totalWeighted;
                cumulativeProbability += probability;
                if (randomValue < cumulativeProbability)
                {
                    move = bookMoves[i].move;
                    return true;
                }
            }

            // Fallback.
            move = bookMoves[0].move;
            return true;
        }
    }
}