using static Chess.Core.SQ;
using System;
using System.Linq;
using Unity.Mathematics;

namespace Chess.Core
{
    public static class MoveGenerator
    {   
        public const int MaxMoves = 218;
        public const int MaxPieceMoves = 27;

        public enum PromotionMode { All, QueenOnly, QueenAndKnight }
        public static PromotionMode PromotionsToGenerate { get; set; } = PromotionMode.QueenAndKnight;

        public static Span<Move> GenerateAllMoves(Board board, bool onlyCaptures)
        {
            Move[] moveArray = new Move[MaxMoves];
            Span<Move> moves = moveArray.AsSpan();
            GenerateAllMoves(board, moves, onlyCaptures, out int moveCount);
            return moves.Slice(0, moveCount);
        }

        public static void GenerateAllMoves(Board board, Span<Move> moves, bool onlyCaptures, out int moveCount)
        {   
            moveCount = 0;
            for (int square = 0; square < Board.Squares; square++) 
            {   
                int piece = board.Chessboard[square];

                if (piece == Piece.None) continue;

                int pieceColor = Piece.PieceColor(piece);
                int pieceType = Piece.PieceType(piece);

                if (pieceColor == board.CurrentGameState.ColorToMove)
                {
                    switch (pieceType)
                    {
                        case Piece.Pawn:
                            GeneratePawnMoves(square, board, moves, onlyCaptures, ref moveCount);
                            break;
                        case Piece.Knight:
                            GenerateKnightMoves(square, board, moves, onlyCaptures, ref moveCount);
                            break;
                        case Piece.Bishop:
                            GenerateBishopMoves(square, board, moves, onlyCaptures, ref moveCount);
                            break;
                        case Piece.Rook:
                            GenerateRookMoves(square, board, moves, onlyCaptures, ref moveCount);
                            break;
                        case Piece.Queen:
                            GenerateQueenMoves(square, board, moves, onlyCaptures, ref moveCount);
                            break;
                        case Piece.King:
                            GenerateKingMoves(square, board, moves, onlyCaptures, ref moveCount);
                            break;
                    }
                }
            }

            // After generating all moves, filter out illegal ones
            moveCount = FilterOutIllegalMoves(board, moves.Slice(0, moveCount));
        }


        // Generates the moves that the piece currently on a specific square has
        public static Span<Move> GenerateSquareMoves(Board board, int square, bool onlyCaptures)
        {
            Move[] moveArray = new Move[MaxPieceMoves];
            Span<Move> moves = moveArray.AsSpan();

            int moveCount = 0;
            switch (board.Chessboard[square])
                {
                    case Piece.Pawn:
                        GeneratePawnMoves(square, board, moves, onlyCaptures, ref moveCount);
                        break;
                    case Piece.Knight:
                        GenerateKnightMoves(square, board, moves, onlyCaptures, ref moveCount);
                        break;
                    case Piece.Bishop:
                        GenerateBishopMoves(square, board, moves, onlyCaptures, ref moveCount);
                        break;
                    case Piece.Rook:
                        GenerateRookMoves(square, board, moves, onlyCaptures, ref moveCount);
                        break;
                    case Piece.Queen:
                        GenerateQueenMoves(square, board, moves, onlyCaptures, ref moveCount);
                        break;
                    case Piece.King:
                        GenerateKingMoves(square, board, moves, onlyCaptures, ref moveCount);
                        break;
                }
            return moves.Slice(0, moveCount);
        }

        public static void GeneratePawnMoves(int square, Board board, Span<Move> moves, bool onlyCaptures, ref int moveCount)
        {
            int direction = board.IsWhiteToMove ? 1 : -1;
            int singlePush = square + direction * 8;
            int doublePush = square + direction * 16;
            int rank = BoardHelper.RankIndex(square);
            int file = BoardHelper.FileIndex(square);

            int startSquare = square;
            int targetSquare;

            int piece = board.Chessboard[square];
            int pieceColor = Piece.PieceColor(piece);

            bool lastMoveDouble = (board.CurrentGameState.EnPassantFile == -1) ? false : true;

            // Single and double pushes
            if (!onlyCaptures)
            {   
                if (BoardHelper.SquareInBounds(singlePush))
                {
                    bool noPieceInfront = board.Chessboard[singlePush] == Piece.None;
                    bool noPieceTwoInfront = false;
                    if (BoardHelper.SquareInBounds(doublePush))
                        noPieceTwoInfront = board.Chessboard[doublePush] == Piece.None;

                    bool hasNotMoved = (rank == 1 && pieceColor == 0) || (rank == 6 && pieceColor == 1);

                    // Single push
                    if (noPieceInfront)
                    {   
                        targetSquare = singlePush;
                        bool isPromotion = (BoardHelper.RankIndex(targetSquare) == 0 || BoardHelper.RankIndex(targetSquare) == 7);

                        if (isPromotion) 
                            GeneratePromotionMoves(startSquare, targetSquare, board, moves, ref moveCount);
                        else 
                            moves[moveCount++] = new Move(startSquare, targetSquare, Move.NoFlag);
                    }
                    
                    // Double push
                    if (noPieceInfront && noPieceTwoInfront && hasNotMoved)
                    {   
                        targetSquare = doublePush;
                        moves[moveCount++] = new Move(startSquare, targetSquare, Move.PawnTwoUpFlag);
                    }
                }
                
            }
            
            // Captures
            int leftCaptureSquare = startSquare + (7 * direction);
            int rightCaptureSquare = startSquare + (9 * direction);

            // Capture square is inbounds and does not wrap to the other side of the board
            if (BoardHelper.SquareInBounds(leftCaptureSquare) && math.abs(leftCaptureSquare / 8 - startSquare / 8) == 1)
            {
                    
                int leftPiece = board.Chessboard[leftCaptureSquare];
                bool canLeftCapture = Piece.PiecesAreDifferentColor(piece, leftPiece);
                
                if (canLeftCapture)
                {
                    targetSquare = leftCaptureSquare;
                    bool isPromotion = (BoardHelper.RankIndex(targetSquare) == 0 || BoardHelper.RankIndex(targetSquare) == 7);

                    if (isPromotion)
                        GeneratePromotionMoves(startSquare, targetSquare, board, moves, ref moveCount);
                    else 
                        moves[moveCount++] = new Move(startSquare, targetSquare, Move.NoFlag);
                }
            }
            // Capture square is inbounds and does not wrap to the other side of the board
            if (BoardHelper.SquareInBounds(rightCaptureSquare) && math.abs(rightCaptureSquare / 8 - startSquare / 8) == 1)
            {   
                int rightPiece = board.Chessboard[rightCaptureSquare];
                bool canRightCapture = Piece.PiecesAreDifferentColor(piece, rightPiece);

                if (canRightCapture)
                {
                    targetSquare = rightCaptureSquare;
                    bool isPromotion = (BoardHelper.RankIndex(targetSquare) == 0 || BoardHelper.RankIndex(targetSquare) == 7);

                    // Adds each possible promotion
                    // TODO change promotion based on settings (only queens, queen and knight, or all four)
                    if (isPromotion)
                        GeneratePromotionMoves(startSquare, targetSquare, board, moves, ref moveCount);
                    else 
                        moves[moveCount++] = new Move(startSquare, targetSquare, Move.NoFlag);
                }
            }

            // En passant
            if (lastMoveDouble)
            {   
                int enPassantFile = board.CurrentGameState.EnPassantFile;
                int enPassantRank = (pieceColor == 0) ? 4 : 3;

                bool isCaptureOnLeft = file - direction == enPassantFile;
                bool isCaptureOnRight = file + direction == enPassantFile;
                bool canCapture = (rank == enPassantRank && (isCaptureOnLeft || isCaptureOnRight));

                if (canCapture)
                {
                    targetSquare = startSquare + direction * (isCaptureOnLeft ? 7 : 9);
                    moves[moveCount++] = new Move(startSquare, targetSquare, Move.EnPassantCaptureFlag);
                }
            }
        }

        public static void GeneratePromotionMoves(int startSquare, int targetSquare, Board board, Span<Move> moves, ref int moveCount)
        {
            // Adds the variations of pawn promotions depending on the promotion mode
            // Always add queen promotion move
            moves[moveCount++] = new Move(startSquare, targetSquare, Move.PromoteToQueenFlag);

            // Add all four promotions
            if (PromotionsToGenerate == PromotionMode.All)
            {
                moves[moveCount++] = new Move(startSquare, targetSquare, Move.PromoteToKnightFlag);
                moves[moveCount++] = new Move(startSquare, targetSquare, Move.PromoteToRookFlag);
                moves[moveCount++] = new Move(startSquare, targetSquare, Move.PromoteToBishopFlag);
            }
            // Add knight and queen promotion mode
            else if (PromotionsToGenerate == PromotionMode.QueenAndKnight)
            {
                moves[moveCount++] = new Move(startSquare, targetSquare, Move.PromoteToKnightFlag);
            }
        }


        public static void GenerateKnightMoves(int square, Board board, Span<Move> moves, bool onlyCaptures, ref int moveCount)
        {   
            int directionCount = BoardHelper.KnightCoorDirections.Length;

            // Loop through each possible knight move offset
            for (int index = 0; index < directionCount; index++)
            {   
                int rankOffset = BoardHelper.KnightCoorDirections[index].rank;
                int fileOffset = BoardHelper.KnightCoorDirections[index].file;

                int targetRank = square / 8 + rankOffset;
                int targetFile = square % 8 + fileOffset;

                Coordinate targetCoor = new Coordinate(targetRank, targetFile);
                int targetSquare = targetCoor.SquareIndex();
                
                // Move is inbounds
                if (!targetCoor.IsInBounds())
                    continue; 

                int movePiece = board.Chessboard[square];
                int movePieceColor = Piece.PieceColor(movePiece);

                int targetPiece = board.Chessboard[targetSquare];
                int targetPieceColor = Piece.PieceColor(targetPiece);

                // Do not add if only captures are being generated and the target piece is none
                if (onlyCaptures && targetPiece == Piece.None)
                    continue;

                // Add move
                if (movePieceColor != targetPieceColor || targetPiece == Piece.None)
                    moves[moveCount++] = new Move(square, targetSquare, Move.NoFlag);
            }
        }

        public static void GenerateBishopMoves(int square, Board board, Span<Move> moves, bool onlyCaptures, ref int moveCount)
        {
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
                        moves[moveCount++] = new Move(startSquare, targetSquare, Move.NoFlag);
                        break;
                    }

                    if (!onlyCaptures)
                    {
                        // Non-capture move
                        if (Piece.IsNone(targetPiece))
                        {
                            moves[moveCount++] = new Move(startSquare, targetSquare, Move.NoFlag);
                        }
                        // Ally Piece
                        else if (Piece.PiecesAreSameColor(piece, targetPiece)) break;
                    }
                    targetSquare += direction;
                }
            }
        }

        public static void GenerateRookMoves(int square, Board board, Span<Move> moves, bool onlyCaptures, ref int moveCount)
        {
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
                        moves[moveCount++] = new Move(startSquare, targetSquare, Move.NoFlag);
                        break;
                    }

                    if (!onlyCaptures)
                    {
                        // Non-capture move
                        if (Piece.IsNone(targetPiece))
                        {
                            moves[moveCount++] = new Move(startSquare, targetSquare, Move.NoFlag);
                        }
                        // Ally Piece
                        else if (Piece.PiecesAreSameColor(piece, targetPiece)) break;
                    }
                    targetSquare += direction;
                }
            }
        }

        public static void GenerateQueenMoves(int square, Board board, Span<Move> moves, bool onlyCaptures, ref int moveCount)
        {
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
                        moves[moveCount++] = new Move(startSquare, targetSquare, Move.NoFlag);
                        break;
                    }

                    if (!onlyCaptures)
                    {
                        // Non-capture move
                        if (Piece.IsNone(targetPiece))
                        {
                            moves[moveCount++] = new Move(startSquare, targetSquare, Move.NoFlag);
                        }
                        // Ally Piece
                        else if (Piece.PiecesAreSameColor(piece, targetPiece)) break;
                    }
                    targetSquare += direction;
                }
            }
        }

        public static void GenerateKingMoves(int square, Board board, Span<Move> moves, bool onlyCaptures, ref int moveCount)
        {
            int[] kingSquareDirections = BoardHelper.StraightSquareDirections.Concat(BoardHelper.DiagonalSquareDirections).ToArray();
            int directionCount = kingSquareDirections.Length;

            int king = board.Chessboard[square];

            // Normal king moves: move one square in each direction
            for (int index = 0; index < directionCount; index++)
            {
                int direction = kingSquareDirections[index];
                int startSquare = square;
                int targetSquare = square + direction;

                if (BoardHelper.SquareInBounds(targetSquare))
                {
                    // Stops diagonal moves from wrapping around the board
                    if (math.abs(direction) == 7 || math.abs(direction) == 9)
                    {
                        if (math.abs(targetSquare / 8 - startSquare / 8) != math.abs(targetSquare % 8 - startSquare % 8))
                            continue;
                    }
                    // Stops horizontal/vertical from wrapping around the board
                    else 
                    {
                        if (targetSquare / 8 != startSquare / 8 && math.abs(direction) == 1)
                            continue;
                        if (targetSquare % 8 != startSquare % 8 && math.abs(direction) == 8)
                            continue;
                    }

                    int targetPiece = board.Chessboard[targetSquare];

                    // Capture
                    if (Piece.PiecesAreDifferentColor(king, targetPiece))
                    {
                        moves[moveCount++] = new Move(startSquare, targetSquare, Move.NoFlag);
                    }
                    // Non-capture move
                    if (!onlyCaptures && Piece.IsNone(targetPiece))
                    {   
                        moves[moveCount++] = new Move(startSquare, targetSquare, Move.NoFlag);
                    }
                }
            }

            // Castling moves
            if (!onlyCaptures)
            {
            
                if (Piece.IsWhite(king))
                {   
                    int startSquare = square;

                    if (board.CurrentGameState.WhiteKingSideCastlingRight)
                    {
                        int targetSquare = startSquare + 2;
                        if (Piece.IsNone(board.Chessboard[startSquare + 1]) && Piece.IsNone(board.Chessboard[targetSquare]))
                            moves[moveCount++] = new Move(startSquare, targetSquare, Move.CastleFlag);
                    }
                    if (board.CurrentGameState.WhiteQueenSideCastlingRight)
                    {
                        int targetSquare = startSquare - 2;
                        if (Piece.IsNone(board.Chessboard[startSquare - 1]) && Piece.IsNone(board.Chessboard[targetSquare]) && Piece.IsNone(board.Chessboard[startSquare - 3]))
                            moves[moveCount++] = new Move(startSquare, targetSquare, Move.CastleFlag);
                    }
                }
                else 
                {
                    int startSquare = square;

                    if (board.CurrentGameState.BlackKingSideCastlingRight)
                    {
                        int targetSquare = startSquare + 2;
                        if (Piece.IsNone(board.Chessboard[startSquare + 1]) && Piece.IsNone(board.Chessboard[targetSquare]))
                            moves[moveCount++] = new Move(startSquare, targetSquare, Move.CastleFlag);
                    }
                    if (board.CurrentGameState.BlackQueenSideCastlingRight)
                    {
                        int targetSquare = startSquare - 2;
                        if (Piece.IsNone(board.Chessboard[startSquare - 1]) && Piece.IsNone(board.Chessboard[targetSquare]) && Piece.IsNone(board.Chessboard[startSquare - 3]))
                            moves[moveCount++] = new Move(startSquare, targetSquare, Move.CastleFlag);
                    }
                }
            }
        }

        public static Span<Move> GenerateMoves(int square, Board board, bool onlyCaptures)
        {   
            Move[] moveArray = new Move[MaxPieceMoves];
            Span<Move> moves = moveArray.AsSpan();
            GenerateMoves(square, board, moves, onlyCaptures, out int moveCount);
            return moves.Slice(0, moveCount);
        }

        public static void GenerateMoves(int square, Board board, Span<Move> moves, bool onlyCaptures, out int moveCount)
        {
            moveCount = 0;
            int pieceType = Piece.PieceType(board.Chessboard[square]);
            
            switch (pieceType)
            {
                case Piece.Pawn:
                    GeneratePawnMoves(square, board, moves, onlyCaptures, ref moveCount);
                    break;
                case Piece.Knight:
                    GenerateKnightMoves(square, board, moves, onlyCaptures, ref moveCount);
                    break;
                case Piece.Bishop:
                    GenerateBishopMoves(square, board, moves, onlyCaptures, ref moveCount);
                    break;
                case Piece.Rook:
                    GenerateRookMoves(square, board, moves, onlyCaptures, ref moveCount);
                    break;
                case Piece.Queen:
                    GenerateQueenMoves(square, board, moves, onlyCaptures, ref moveCount);
                    break;
                case Piece.King:
                    GenerateKingMoves(square, board, moves, onlyCaptures, ref moveCount);
                    break;
            }

            // After generating moves, filter out illegal ones
            moveCount = FilterOutIllegalMoves(board, moves.Slice(0, moveCount));
        }

        public static int FilterOutIllegalMoves(Board board, Span<Move> moves)
        {
            int legalMoveCount = 0;
            int color = board.CurrentGameState.ColorToMove;

            for (int index = 0; index < moves.Length; index++)
            {
                Move move = moves[index];

                if (move.IsCastling)
                {
                    // Cannot castle if king is currently in check
                    bool isKingInCheckNow = board.IsKingInCheck(board, color);
                    if (isKingInCheckNow)
                        continue;

                    int targetSquare = move.TargetSquare;
                    bool isKingSideCastle = targetSquare == g1 || targetSquare == g8;

                    // Cannot castle if the squares the rook and king will be on are under attack
                    if (isKingSideCastle)
                    {
                        int square1 = (color == 0) ? 5 : 61;
                        int square2 = (color == 0) ? 6 : 62;
                        if (IsSquareUnderAttack(board, square1, color) || IsSquareUnderAttack(board, square2, color))
                            continue;
                    }
                    else {
                        int square1 = (color == 0) ? 2 : 58;
                        int square2 = (color == 0) ? 3 : 59;
                        if (IsSquareUnderAttack(board, square1, color) || IsSquareUnderAttack(board, square2, color))
                            continue;
                    }

                    // Castling is legal
                    moves[legalMoveCount] = move;
                    legalMoveCount++;

                    continue;
                }

                // If the king is in check after the move is made, then the move is not legal
                board.MakeMove(move);
                bool isKingInCheck = board.IsKingInCheck(board, color);
                board.UnMakeMove(move);

                if (!isKingInCheck)
                {   
                    moves[legalMoveCount] = move;
                    legalMoveCount++;
                }
            }
            return legalMoveCount;
        }


        static public bool IsMoveLegal(Board board, Move move)
        {
            int color = board.CurrentGameState.ColorToMove;

            if (move.IsCastling)
            {
                // Cannot castle if king is currently in check
                bool isKingInCheckNow = board.IsKingInCheck(board, color);
                if (isKingInCheckNow)
                    return false;

                int targetSquare = move.TargetSquare;
                bool isKingSideCastle = targetSquare == g1 || targetSquare == g8;

                // Cannot castle if the squares the rook and king will be on are under attack
                if (isKingSideCastle)
                {
                    int square1 = (color == 0) ? 5 : 61;
                    int square2 = (color == 0) ? 6 : 62;
                    if (IsSquareUnderAttack(board, square1, color) || IsSquareUnderAttack(board, square2, color))
                        return false;
                }
                else {
                    int square1 = (color == 0) ? 2 : 58;
                    int square2 = (color == 0) ? 3 : 59;
                    if (IsSquareUnderAttack(board, square1, color) || IsSquareUnderAttack(board, square2, color))
                        return false;
                }

                // Castling is legal
                return true;
            }

            // If the king is in check after the move is made, then the move is not legal
            board.MakeMove(move);
            bool isKingInCheck = board.IsKingInCheck(board, color);
            board.UnMakeMove(move);

            if (!isKingInCheck)
            {   
                return true;
            }

            return false;
        }


        // Checks if an enemy piece is on a square that attacks the given square
        public static bool IsSquareUnderAttack(Board board, int square, int colorToMove)
        {
            int enemyColor = colorToMove == 0 ? 1 : 0;

            // Check for pawn attacks
            int pawnDirection = enemyColor == 0 ? -1 : 1;
            int[] possiblePawnSquares = { square + (7 * pawnDirection), square + (9 * pawnDirection) };
            foreach (var pawnSquare in possiblePawnSquares)
            {
                if (BoardHelper.SquareInBounds(pawnSquare))
                {
                    int piece = board.Chessboard[pawnSquare];
                    if (piece != Piece.None && Piece.PieceType(piece) == Piece.Pawn && Piece.PieceColor(piece) == enemyColor) return true;
                }
            }

            // Check for knight attacks
            foreach (var direction in BoardHelper.KnightCoorDirections)
            {
                int targetRank = square / 8 + direction.rank;
                int targetFile = square % 8 + direction.file;

                Coordinate targetCoor = new Coordinate(targetRank, targetFile);
                int targetSquare = targetCoor.SquareIndex();
                
                if (!targetCoor.IsInBounds())
                    continue; 

                int piece = board.Chessboard[targetSquare];
                if (piece != Piece.None && Piece.PieceType(piece) == Piece.Knight && Piece.PieceColor(piece) == enemyColor) return true;
            }
            
            // Check for sliding pieces (Bishop, Rook, Queen)
            foreach (var direction in BoardHelper.StraightSquareDirections.Concat(BoardHelper.DiagonalSquareDirections))
            {
                int targetSquare = square + direction;
                
                while (BoardHelper.SquareInBounds(targetSquare))
                {
                    // Stops diagonal moves from wrapping around the board
                    if (math.abs(direction) == 7 || math.abs(direction) == 9)
                    {
                        if (math.abs(targetSquare / 8 - square / 8) != math.abs(targetSquare % 8 - square % 8))
                            break;
                    }
                    // Stops horizontal/vertical from wrapping around the board
                    else 
                    {
                        if (targetSquare / 8 != square / 8 && math.abs(direction) == 1)
                            break;
                        if (targetSquare % 8 != square % 8 && math.abs(direction) == 8)
                            break;
                    }
                
                    int piece = board.Chessboard[targetSquare];
                    if (piece != Piece.None)
                    {
                        if (Piece.PieceColor(piece) != enemyColor)
                            break;

                        int pieceType = Piece.PieceType(piece);
                        if (IsSlidingAttack(direction, pieceType)) return true;
                        break;
                    }
                    targetSquare += direction;
                }
            }

            // Check for king attacks (adjacent squares)
            foreach (var direction in BoardHelper.StraightSquareDirections.Concat(BoardHelper.DiagonalSquareDirections))
            {
                int target = square + direction;
                if (BoardHelper.SquareInBounds(target))
                {
                    int piece = board.Chessboard[target];
                    if (piece != Piece.None && Piece.PieceType(piece) == Piece.King && Piece.PieceColor(piece) == enemyColor)
                        return true;
                }
            }

            return false;
        }

        private static bool IsSlidingAttack(int direction, int pieceType)
        {
            if (direction == 1 || direction == -1 || direction == 8 || direction == -8) // Rook directions
            {
                return pieceType == Piece.Rook || pieceType == Piece.Queen;
            }
            else // Diagonal directions
            {
                return pieceType == Piece.Bishop || pieceType == Piece.Queen;
            }
        }
    }
}