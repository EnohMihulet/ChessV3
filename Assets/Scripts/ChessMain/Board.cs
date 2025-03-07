using System;
using System.Collections.Generic;
using static Chess.Core.SQ;
using UnityEngine;

namespace Chess.Core
{
    public class Board
    {
        // Board represented by array of 64 integers
        public int[] Chessboard;
        public const int ranks = 8;
        public const int files = 8;
        public const int Squares = ranks * files;
        // Positions of kings
        public int[] Kings;

        // Stores gamestate variables like castling rights, en passant file, color to move, etc.
        public GameState CurrentGameState;
        // Result of the game (in progress, stalemate, black is mated, white is mated, etc.)
        public GameResult.EndResult CurrentEndResult;

        public bool IsWhiteToMove => CurrentGameState.ColorToMove == 0 ? true: false;
        public int MoveColor => IsWhiteToMove ? 0 : 8; // Used to create pieces
        public int OpponentColor => IsWhiteToMove ? 8 : 0; // Used to create pieces
        public const int WhiteIndex = 0;
        public const int BlackIndex = 1;
        public int MoveColorIndex => IsWhiteToMove ? 0 : 1;
        public int OpponentColorIndex  => IsWhiteToMove ? 1 : 0;

        public ulong ZobristHash => CurrentGameState.ZobristHash; // ulong storing a position
        public int ColorToMove => CurrentGameState.ColorToMove;
        public List<Move> AllGameMoves; // All moves made during the game
        public Stack<ulong> RepetitionPositionHistory; // All positions used to check for draw by repetition
        public List<int> CapturedPieces;
        private Stack<GameState> GameStateHistory = new Stack<GameState>(); // Allows for easy make/unmake of moves

        // Piece lists
        public PieceList[] AllPieces;

        // Bitboards
        public ulong[] AllBitBoards; // Indexed by piece type


        public Board()
        {
            // Initialize Chessboard
            Chessboard = new int[64];
        }


        public void MovePiece(int oldSquare, int newSquare) 
        {   
            AllPieces[Chessboard[oldSquare]].MovePiece(oldSquare, newSquare);

            Chessboard[newSquare] = Chessboard[oldSquare];
            Chessboard[oldSquare] = Piece.None;
            
        }

        // Make a move and update the state of the game
        // Record move determines whether or not to store move
        public void MakeMove(Move move, bool recordMove = false, bool inSearch = false)
        {
            int startSquare = move.StartSquare;
            int targetSquare = move.TargetSquare;

            // Move flags
            bool isCastling = move.IsCastling;
            bool isEnPassantCapture = move.IsEnPassantCapture;
            bool isDoubleMove = move.IsDoubleMove;
            bool isPromotion = move.IsPromotion;

            // Pieces
            int movedPiece = Chessboard[startSquare];
            int movedPieceType = Piece.PieceType(movedPiece);
            int capturedPiece = isEnPassantCapture ? Piece.MakePiece(Piece.Pawn, OpponentColor) : Chessboard[targetSquare];

            // Game state 
            int prevCastlingRights = CurrentGameState.CastlingRights;
			int prevEnPassantFile = CurrentGameState.EnPassantFile;
			int newCastlingRights = CurrentGameState.CastlingRights;
			int newEnPassantFile = -1;
            ulong newZobristHash = CurrentGameState.ZobristHash;
            int newHalfmoveClock = CurrentGameState.HalfmoveClock + 1;

            // Reset halfmove clock
            newHalfmoveClock = movedPieceType == Piece.Pawn ? 0 : newHalfmoveClock;

            // Move piece (capture if move is not en passant)
            MovePiece(startSquare, targetSquare);

            // CAPTURES
            if (!Piece.IsNone(capturedPiece))
            {
                int captureSquare = targetSquare;
                newHalfmoveClock = 0; // Reset halfmove clock

                // En passant capture
                if (isEnPassantCapture)
                {
                    captureSquare = targetSquare + (IsWhiteToMove ? -8 : 8);
                    Chessboard[captureSquare] = Piece.None;
                }
                // Update Piece Lists
                AllPieces[capturedPiece].RemovePiece(captureSquare);

                // Update zobrist hash
                newZobristHash ^= Zobrist.PieceSquareArray[capturedPiece, captureSquare]; // Remove captured piece position

                // Add captured piece to captured pieces list
                if (recordMove)
                    CapturedPieces.Add(capturedPiece);
                
            }
            // KING MOVE
            if (movedPieceType == Piece.King) 
            {
                Kings[MoveColorIndex] = targetSquare;
                newCastlingRights &= IsWhiteToMove ? 0b1100 : 0b0011;

                // Handle castling
                if (isCastling)
                {
                    bool isKingSideCastle = targetSquare == g1 || targetSquare == g8;
                    int RookStartSquare = isKingSideCastle ? targetSquare + 1 : targetSquare - 2;
                    int RookTargetSquare = isKingSideCastle ? targetSquare - 1 : targetSquare  + 1;

                    MovePiece(RookStartSquare, RookTargetSquare);

                    // Reset halfmove clock
                    newHalfmoveClock = 0;

                    // Update zobrist hash
                    // Remove old rook position and add the new rook position
                    newZobristHash ^= Zobrist.PieceSquareArray[Chessboard[RookStartSquare], RookStartSquare];
                    newZobristHash ^= Zobrist.PieceSquareArray[Chessboard[RookTargetSquare], RookTargetSquare];
                }
            }
            // PAWN DOUBLE MOVE
            else if (isDoubleMove)
            {
                // Update en passant file
                newEnPassantFile = BoardHelper.FileIndex(startSquare);

                // Update zobrist hash 
                newZobristHash ^= Zobrist.EnPassantFile[newEnPassantFile + 1];
            }
            // PROMOTION
            else if (isPromotion)
            {
                int promotionType =  move.PromotionPieceType;
                int promotionPiece = promotionType | MoveColor;

                // Update piece lists
                AllPieces[movedPiece].RemovePiece(targetSquare);
                AllPieces[promotionPiece].AddPiece(targetSquare);

                Chessboard[targetSquare] = promotionPiece;
            }

            // Update new game state variables
            int capturedPieceType = Piece.PieceType(capturedPiece);
            int newFullmoveCount = CurrentGameState.FullmoveCount + (IsWhiteToMove ? 0 : 1);

        	// Update new game state castling rights
			if (prevCastlingRights != 0)
			{
				// Any piece moving to/from rook square removes castling right for that side
				if (targetSquare == h1 || startSquare == h1)
					newCastlingRights &= GameState.ClearWhiteKingsideMask;
				else if (targetSquare == a1 || startSquare == a1)
					newCastlingRights &= GameState.ClearWhiteQueensideMask;
				if (targetSquare == h8 || startSquare == h8)
					newCastlingRights &= GameState.ClearBlackKingsideMask;
				else if (targetSquare == a8 || startSquare == a8)
					newCastlingRights &= GameState.ClearBlackQueensideMask;
			}

            // Update zobrist hash with new piece position and side to move
            newZobristHash ^= Zobrist.SideToMove;
            newZobristHash ^= Zobrist.PieceSquareArray[movedPiece, startSquare]; // Remove old piece position
            newZobristHash ^= Zobrist.PieceSquareArray[movedPiece, targetSquare]; // Add new piece position
			newZobristHash ^= Zobrist.EnPassantFile[prevEnPassantFile + 1]; // Remove old en passant file

            // Update zobrist hash with new castling rights
            if (newCastlingRights != prevCastlingRights)
			{
				newZobristHash ^= Zobrist.CastlingRights[prevCastlingRights]; // remove old castling rights state
				newZobristHash ^= Zobrist.CastlingRights[newCastlingRights]; // add new castling rights state
			}

            // Create the new game-state and push the new gamestate to the stack
            GameState newState = new(OpponentColorIndex, newCastlingRights, newEnPassantFile, capturedPieceType, newHalfmoveClock, newFullmoveCount, newZobristHash);
            GameStateHistory.Push(newState);
			CurrentGameState = newState;

            // Add position to repetition history and add move to move
            // Current result of the game
            if (recordMove)
            {
                RepetitionPositionHistory.Push(newZobristHash);
                CurrentEndResult = GameResult.CurrentGameResult(this);
                AllGameMoves.Add(move);
            }
            
            if (inSearch && !recordMove)
            {
                AllGameMoves.Add(move);
                RepetitionPositionHistory.Push(newZobristHash);
                CurrentEndResult = GameResult.CurrentGameResult(this);
            }
        }   

        public void UnMakeMove(Move move, bool recordMove = false, bool inSearch = false)
        {   
            CurrentGameState.ChangeColorToMove();

            // Move info
            int movedFrom = move.StartSquare;
            int movedTo = move.TargetSquare;
            
            bool wasEnPassant = move.IsEnPassantCapture;
            bool wasPromotion = move.IsPromotion;
            bool wasCapture = CurrentGameState.CapturedPieceType != Piece.None;
            bool wasCastling = move.IsCastling;

            int movedPiece = wasPromotion ? Piece.MakePiece(Piece.Pawn, MoveColor) : Chessboard[movedTo];
            int movedPieceType = Piece.PieceType(movedPiece);
            int capturedPieceType = CurrentGameState.CapturedPieceType;

            // PROMOTION
            if (wasPromotion)
            {
                // Undo promotion in piece lists
                AllPieces[Chessboard[movedTo]].RemovePiece(movedTo);
                AllPieces[movedPiece].AddPiece(movedTo);

                Chessboard[movedTo] = Piece.MakePiece(Piece.Pawn, MoveColor);
            }
                

            MovePiece(movedTo, movedFrom);

            // CAPTURES
            if (wasCapture)
            {   
                int capturedSquare = movedTo;
                int capturedPiece = Piece.MakePiece(capturedPieceType, OpponentColor);
                
                if (wasEnPassant)
                    capturedSquare = movedTo + (IsWhiteToMove ? -8 : 8);

                AllPieces[capturedPiece].AddPiece(capturedSquare);

                Chessboard[capturedSquare] = capturedPiece;

                if (recordMove)
                    CapturedPieces.Remove(capturedPiece);
            }
            // KING MOVES
            if (movedPieceType == Piece.King)
            {
                Kings[MoveColorIndex] = movedFrom;

                // Undo castling
                if (wasCastling)
                {
                    bool isKingSideCastle = movedTo == g1 || movedTo == g8;
                    int rookStartSquare = isKingSideCastle ? movedTo + 1 : movedTo - 2;
                    int rookTargetSquare = isKingSideCastle ? movedTo - 1 : movedTo + 1;

                    MovePiece(rookTargetSquare, rookStartSquare);
                }
            }

            // Restore the game state to state before the move was made
            GameStateHistory.Pop();
            CurrentGameState = GameStateHistory.Peek();

            // Remove previous position from repetition history and remove previous move from move history
            // Current result of the game
            if (recordMove)
            {
                RepetitionPositionHistory.Pop();
                CurrentEndResult = GameResult.CurrentGameResult(this);
                AllGameMoves.Remove(move);
            }

            if (inSearch && !recordMove)
            {
                AllGameMoves.Remove(move);
                RepetitionPositionHistory.Pop();
                CurrentEndResult = GameResult.CurrentGameResult(this);
            }
        }

        public bool IsKingInCheck(Board board, int kingColor)
        {   
            int kingSquare = Kings[kingColor];

            return MoveGenerator.IsSquareUnderAttack(board, kingSquare, kingColor);
        }

        // Load the starting position
		public void LoadStartPosition()
		{
			LoadPosition(FenUtility.StartPositionFEN);
		}

		public void LoadPosition(string fen)
		{
			FenUtility.PositionInfo posInfo = FenUtility.PositionFromFen(fen);
			LoadPosition(posInfo);
		}

		public void LoadPosition(FenUtility.PositionInfo posInfo)
		{
			Initialize();

			// Load pieces into board array and piece lists
			for (int squareIndex = 0; squareIndex < 64; squareIndex++)
			{
				int piece = posInfo.squares[squareIndex];
				int pieceType = Piece.PieceType(piece);
				int colourIndex = Piece.IsWhite(piece) ? WhiteIndex : BlackIndex;
				Chessboard[squareIndex] = piece;

				if (piece != Piece.None)
				{
                    bitboardUtil.Set(AllBitBoards[pieceType], squareIndex);
                    
					if (pieceType == Piece.King)
					{
						Kings[colourIndex] = squareIndex;
					}

                    AllPieces[piece].AddPiece(squareIndex);
				}
			}

			// Side to move
			int colorToMove = posInfo.whiteToMove ? 0 : 1;

			// Create gamestate
			int whiteCastle = ((posInfo.whiteCastleKingside) ? 1 << 0 : 0) | ((posInfo.whiteCastleQueenside) ? 1 << 1 : 0);
			int blackCastle = ((posInfo.blackCastleKingside) ? 1 << 2 : 0) | ((posInfo.blackCastleQueenside) ? 1 << 3 : 0);
			int castlingRights = whiteCastle | blackCastle;

			// Set game state (note: calculating zobrist key relies on current game state)
			CurrentGameState = new GameState(colorToMove, castlingRights, posInfo.epFile, 0, posInfo.fiftyMovePlyCount, posInfo.moveCount, 0);
			ulong zobristKey = Zobrist.ZobristHash(this);
			CurrentGameState = new GameState(colorToMove, castlingRights, posInfo.epFile, 0, posInfo.fiftyMovePlyCount, posInfo.moveCount, zobristKey);

			RepetitionPositionHistory.Push(zobristKey);

			GameStateHistory.Push(CurrentGameState);
		}

        void Initialize()
		{
			AllGameMoves = new List<Move>();

            // Initialize CurrentGameState
            CurrentGameState = new GameState(1, 0b1111, 0, -1, 0, 0, 0);

            Kings = new int[2]; // Assuming two kings
			
			RepetitionPositionHistory = new Stack<ulong>(capacity: 64);
			GameStateHistory = new Stack<GameState>(capacity: 64);
            CapturedPieces = new List<int>(capacity: 32);
            AllBitBoards = new ulong[Piece.BlackKing + 1];

            AllPieces = new PieceList[15];

            // White Pieces
            AllPieces[1] = new PieceList(8);
            AllPieces[2] = new PieceList(10);
            AllPieces[3] = new PieceList(10);
            AllPieces[4] = new PieceList(10);
            AllPieces[5] = new PieceList(9);
            AllPieces[6] = new PieceList(1);
            // Black Pieces
            AllPieces[9] = new PieceList(8);
            AllPieces[10] = new PieceList(10);
            AllPieces[11] = new PieceList(10);
            AllPieces[12] = new PieceList(10);
            AllPieces[13] = new PieceList(9);
            AllPieces[14] = new PieceList(1);

            AllPieces[0] = new PieceList(0);
            AllPieces[7] = new PieceList(0);
            AllPieces[8] = new PieceList(0);
        }
    }
}
