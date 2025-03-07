﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace Chess.Core
{
	// Helper class for dealing with FEN strings
	// Taken from Sebasten Lague w/ a few small changes
	public static class FenUtility
	{
		public const string StartPositionFEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

		// Load position from fen string
		public static PositionInfo PositionFromFen(string fen)
		{
			PositionInfo loadedPositionInfo = new(fen);
			return loadedPositionInfo;
		}

		/// <summary>
		/// Get the fen string of the current position
		/// When alwaysIncludeEPSquare is true the en passant square will be included
		/// in the fen string even if no enemy pawn is in a position to capture it.
		/// </summary>
		public static string CurrentFen(Board board, bool alwaysIncludeEPSquare = true)
		{
			string fen = "";
			for (int rank = 7; rank >= 0; rank--)
			{
				int numEmptyFiles = 0;
				for (int file = 0; file < 8; file++)
				{
					int i = rank * 8 + file;
					int piece = board.Chessboard[i];
					if (piece != 0)
					{
						if (numEmptyFiles != 0)
						{
							fen += numEmptyFiles;
							numEmptyFiles = 0;
						}
						bool isBlack = Piece.IsBlack(piece);
						int pieceType = Piece.PieceType(piece);
						char pieceChar = ' ';
						switch (pieceType)
						{
							case Piece.Rook:
								pieceChar = 'R';
								break;
							case Piece.Knight:
								pieceChar = 'N';
								break;
							case Piece.Bishop:
								pieceChar = 'B';
								break;
							case Piece.Queen:
								pieceChar = 'Q';
								break;
							case Piece.King:
								pieceChar = 'K';
								break;
							case Piece.Pawn:
								pieceChar = 'P';
								break;
						}
						fen += (isBlack) ? pieceChar.ToString().ToLower() : pieceChar.ToString();
					}
					else
					{
						numEmptyFiles++;
					}

				}
				if (numEmptyFiles != 0)
				{
					fen += numEmptyFiles;
				}
				if (rank != 0)
				{
					fen += '/';
				}
			}

			// Side to move
			fen += ' ';
			fen += (board.IsWhiteToMove) ? 'w' : 'b';

			// Castling
			bool whiteKingside = (board.CurrentGameState.CastlingRights & 1) == 1;
			bool whiteQueenside = (board.CurrentGameState.CastlingRights >> 1 & 1) == 1;
			bool blackKingside = (board.CurrentGameState.CastlingRights >> 2 & 1) == 1;
			bool blackQueenside = (board.CurrentGameState.CastlingRights >> 3 & 1) == 1;
			fen += ' ';
			fen += (whiteKingside) ? "K" : "";
			fen += (whiteQueenside) ? "Q" : "";
			fen += (blackKingside) ? "k" : "";
			fen += (blackQueenside) ? "q" : "";
			fen += ((board.CurrentGameState.CastlingRights) == 0) ? "-" : "";

			// En-passant
			fen += ' ';
			int epFileIndex = board.CurrentGameState.EnPassantFile;
			int epRankIndex = (board.IsWhiteToMove) ? 5 : 2;

			bool isEnPassant = epFileIndex != -1;
			bool includeEP = alwaysIncludeEPSquare || EnPassantCanBeCaptured(epFileIndex, epRankIndex, board);
			if (isEnPassant && includeEP)
			{
				fen += BoardHelper.SquareNameFromCoordinate(epFileIndex, epRankIndex);
			}
			else
			{
				fen += '-';
			}

			// 50 move counter
			fen += ' ';
			fen += board.CurrentGameState.HalfmoveClock;

			// Full-move count (should be one at start, and increase after each move by black)
			fen += ' ';
			fen += board.CurrentGameState.FullmoveCount;

			return fen;
		}

		static bool EnPassantCanBeCaptured(int epFileIndex, int epRankIndex, Board board)
		{
			Coordinate captureFromA = new Coordinate(epFileIndex - 1, epRankIndex + (board.IsWhiteToMove ? -1 : 1));
			Coordinate captureFromB = new Coordinate(epFileIndex + 1, epRankIndex + (board.IsWhiteToMove ? -1 : 1));
			int epCaptureSquare = new Coordinate(epFileIndex, epRankIndex).SquareIndex();
			int friendlyPawn = Piece.MakePiece(Piece.Pawn, board.MoveColor);

			return CanCapture(captureFromA) || CanCapture(captureFromB);


			bool CanCapture(Coordinate from)
			{
				int fromSquare = from.SquareIndex();
				if (!from.IsInBounds())
					return false;

				bool isPawnOnSquare = board.Chessboard[fromSquare] == friendlyPawn;
				if (from.IsInBounds() && isPawnOnSquare)
				{
					Move move = new Move(fromSquare, epCaptureSquare, Move.EnPassantCaptureFlag);
					board.MakeMove(move);
					bool wasLegalMove = !board.IsKingInCheck(board, board.CurrentGameState.ColorToMove);

					board.UnMakeMove(move);
					return wasLegalMove;
				}

				return false;
			}
		}

				public static string FlipFen(string fen)
		{
			string flippedFen = "";
			string[] sections = fen.Split(' ');

			List<char> invertedFenChars = new();
			string[] fenRanks = sections[0].Split('/');

			for (int i = fenRanks.Length - 1; i >= 0; i--)
			{
				string rank = fenRanks[i];
				foreach (char c in rank)
				{
					flippedFen += InvertCase(c);
				}
				if (i != 0)
				{
					flippedFen += '/';
				}
			}

			flippedFen += " " + (sections[1][0] == 'w' ? 'b' : 'w');
			string castlingRights = sections[2];
			string flippedRights = "";
			foreach (char c in "kqKQ")
			{
				if (castlingRights.Contains(c))
				{
					flippedRights += InvertCase(c);
				}
			}
			flippedFen += " " + (flippedRights.Length == 0 ? "-" : flippedRights);

			string ep = sections[3];
			string flippedEp = ep[0] + "";
			if (ep.Length > 1)
			{
				flippedEp += ep[1] == '6' ? '3' : '6';
			}
			flippedFen += " " + flippedEp;
			flippedFen += " " + sections[4] + " " + sections[5];


			return flippedFen;

			char InvertCase(char c)
			{
				if (char.IsLower(c))
				{
					return char.ToUpper(c);
				}
				return char.ToLower(c);
			}
		}
		

		public readonly struct PositionInfo
		{
			public readonly string fen;
			public readonly ReadOnlyCollection<int> squares;

			// Castling rights
			public readonly bool whiteCastleKingside;
			public readonly bool whiteCastleQueenside;
			public readonly bool blackCastleKingside;
			public readonly bool blackCastleQueenside;
			// En passant file (0 is a-file, 7 is h-file, -1 means none)
			public readonly int epFile;
			public readonly bool whiteToMove;
			// Number of half-moves since last capture or pawn advance
			// (starts at 0 and increments after each player's move)
			public readonly int fiftyMovePlyCount;
			// Total number of moves played in the game
			// (starts at 1 and increments after black's move)
			public readonly int moveCount;

			public PositionInfo(string fen)
			{
				this.fen = fen;
				int[] squarePieces = new int[64];

				string[] sections = fen.Split(' ');

				int file = 0;
				int rank = 7;

				foreach (char symbol in sections[0])
				{
					if (symbol == '/')
					{
						file = 0;
						rank--;
					}
					else
					{
						if (char.IsDigit(symbol))
						{
							file += (int)char.GetNumericValue(symbol);
						}
						else
						{
							int pieceColour = (char.IsUpper(symbol)) ? Piece.White : Piece.Black;
							int pieceType = char.ToLower(symbol) switch
							{
								'k' => Piece.King,
								'p' => Piece.Pawn,
								'n' => Piece.Knight,
								'b' => Piece.Bishop,
								'r' => Piece.Rook,
								'q' => Piece.Queen,
								_ => Piece.None
							};

							squarePieces[rank * 8 + file] = pieceType | pieceColour;
							file++;
						}
					}
				}

				squares = new(squarePieces);

				whiteToMove = (sections[1] == "w");

				string castlingRights = sections[2];
				whiteCastleKingside = castlingRights.Contains('K');
				whiteCastleQueenside = castlingRights.Contains('Q');
				blackCastleKingside = castlingRights.Contains('k');
				blackCastleQueenside = castlingRights.Contains('q');

				// Default values
				epFile = -1;
				fiftyMovePlyCount = 0;
				moveCount = 0;

				if (sections.Length > 3)
				{
					string enPassantFileName = sections[3][0].ToString();
					if (BoardHelper.fileNames.Contains(enPassantFileName))
					{
						epFile = BoardHelper.fileNames.IndexOf(enPassantFileName);
					}
				}

				// Half-move clock
				if (sections.Length > 4)
				{
					int.TryParse(sections[4], out fiftyMovePlyCount);
				}
				// Full move number
				if (sections.Length > 5)
				{
					int.TryParse(sections[5], out moveCount);
				}
			}
		}
	}
}