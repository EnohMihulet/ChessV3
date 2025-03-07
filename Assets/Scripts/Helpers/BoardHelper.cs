using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Chess.Core
{
    public static class BoardHelper
    {
        public const string fileNames = "abcdefgh";
		public const string rankNames = "12345678";

        public static readonly Coordinate[] StaightCoorDirections = { new Coordinate(-1, 0), new Coordinate(1, 0), new Coordinate(0, 1), new Coordinate(0, -1) };
		public static readonly Coordinate[] DiagonalCoorDirections = { new Coordinate(-1, 1), new Coordinate(1, 1), new Coordinate(1, -1), new Coordinate(-1, -1) };
        public static readonly Coordinate[] KnightCoorDirections = 
		{ new Coordinate(2, 1), new Coordinate(1, 2), new Coordinate(-2, 1), new Coordinate(-1, 2), new Coordinate(2, -1), new Coordinate(1, -2), new Coordinate(-2, -1), new Coordinate(-1, -2)};

        public static readonly int[] StraightSquareDirections = {-1, 1, 8, -8};
        public static readonly int[] DiagonalSquareDirections = {7, 9, -9, -7};
        public static readonly int[] KnightSquareDirections = {15, 17, 10, 6, -15, -17, -10, -6};

        // Rank (0 to 7) of square 
		public static int RankIndex(int square)
		{
			return square >> 3;
		}

		// File (0 to 7) of square 
		public static int FileIndex(int square)
		{
			return square & 0b000111;
		}

        public static bool SquareInBounds(int square)
        {
           return (square < 64 && square >= 0) ? true : false;
        }

		public static int IndexFromCoord(int file, int rank)
		{
			return rank * 8 + file;
		}

		public static int IndexFromCoord(Coordinate coord)
		{
			return IndexFromCoord(coord.file, coord.rank);
		}

		public static Coordinate CoordFromIndex(int square)
		{
			return new Coordinate(FileIndex(square), RankIndex(square));
		}

		public static string SquareNameFromCoordinate(int file, int rank)
		{
			return fileNames[file] + "" + (rank + 1);
		}

		public static string SquareNameFromIndex(int square)
		{
			return SquareNameFromCoordinate(CoordFromIndex(square));
		}

		public static string SquareNameFromCoordinate(Coordinate coord)
		{
			return SquareNameFromCoordinate(coord.file, coord.rank);
		}

		public static int SquareIndexFromName(string name)
		{
			char fileName = name[0];
			char rankName = name[1];
			int file = fileNames.IndexOf(fileName);
			int rank = rankNames.IndexOf(rankName);
			return IndexFromCoord(file, rank);
		}

		public static bool IsLightSquare(int square)
		{
			return square % 2 == 1;
		}

		public static bool IsDarkSquare(int square)
		{
			return square % 2 == 0;
		}
    }
}

