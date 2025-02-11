using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Chess.Core;
using System;
using Unity.VisualScripting;

namespace Chess.Game
{
	public class HumanPlayer : Player
    {
        public enum InputState
		{
			None,
			PieceSelected,
			DraggingPiece
		}

		InputState currentInputState;
		BoardUI boardUI;
		Camera cam;
		Coordinate selectedPieceCoor;
        Move[] selectedPieceMoves;
        private const float MousePosOffset = .5f;
		public HumanPlayer(Board board, int color)
		{
			boardUI = GameObject.FindObjectOfType<BoardUI>();
			cam = Camera.main;
			this.board = board;
            PlayerColor = color;
            OpponentColor = (color == 0) ? 1 : 0;
            PlayerType = GameManager.PlayerType.Human;
		}

        public override void FindMove()
        {
            HandleInput();
        }

        public void HandleInput(){
            // if (Input.GetMouseButtonDown(0)) LeftClick();
            // if (isDragging && selectedPiece != null) DragPiece();
            // if (Input.GetMouseButtonUp(0)

			Vector2 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);

			if (currentInputState == InputState.None)
			{
				HandlePieceSelection(mousePos);
			}
			else if (currentInputState == InputState.DraggingPiece)
			{
				HandleDrag(mousePos);
			}
			else if (currentInputState == InputState.PieceSelected)
			{
				HandlePointAndClick(mousePos);
			}
            // Right click
			if (Input.GetMouseButtonUp(1))
			{
				CancelPieceSelection();
			}
        }

        public void HandlePieceSelection(Vector2 mousePos)
        {
            if (Input.GetMouseButtonDown(0)) {

                RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

                // Nothing is clicked
                if (hit.collider == null)
                    return;

                GameObject clickedObject = hit.collider.gameObject;

                if (clickedObject.CompareTag("piece")) {
                    // Piece is clicked and the color matches the players color
                    if (GameObjectCoorPosition(clickedObject).CoorColor(board) == PlayerColor)
                    {
                        // Store coordinates of the selected piece
                        selectedPieceCoor = GameObjectCoorPosition(clickedObject);

                        // Stores the possible moves for that selected piece
                        selectedPieceMoves = GenerateSelectedPieceMoves(board, selectedPieceCoor);

                        // Clear previous highlighted moves
                        boardUI.ClearBoard(true, false);
			            boardUI.DrawSquares(true, false);

                        // Highlight the possible moves
                        boardUI.HighlightMoves(board, selectedPieceMoves, selectedPieceCoor);

                        currentInputState = InputState.DraggingPiece;
                    }
                }
            }
        }

        public void HandleDrag(Vector2 mousePos)
        {
            if (Input.GetMouseButtonUp(0))
            {
                Coordinate mouseCoor = MousePositionToCoor(mousePos);

                // Piece clicked, not dragged
                if (selectedPieceCoor.file == mouseCoor.file && mouseCoor.rank == selectedPieceCoor.rank)
                {
                    if (boardUI.Pieces[selectedPieceCoor.SquareIndex()] == null)
                    {
                        CancelPieceSelection();
                        return;
                    }

                    // Put piece back in center of original square
                    Vector3 originalPos = new Vector3(selectedPieceCoor.file, selectedPieceCoor.rank, -1);
                    boardUI.Pieces[selectedPieceCoor.SquareIndex()].transform.localPosition = originalPos;

                    // Wait for new square to be clicked
                    currentInputState = InputState.PieceSelected;
                }
                // Piece dragged and dropped over a new square
                else
                {
                    int pieceTargetSquare = mouseCoor.rank * 8 + mouseCoor.file;

                    Move chosenMove = ChosenLegalMove(pieceTargetSquare);
                    
                    if (chosenMove.IsNull)
                    {
                        // Move is not legal, return piece to original square
                        Vector3 originalPos = new Vector3(selectedPieceCoor.file, selectedPieceCoor.rank, -1);

                        boardUI.Pieces[selectedPieceCoor.SquareIndex()].transform.localPosition = originalPos;
                    }
                    else 
                    {
                        SelectedMove = chosenMove;
                        MoveFound = true;
                    }

                    CancelPieceSelection();
                }
            }
            else
            {   
                if (boardUI.Pieces[selectedPieceCoor.SquareIndex()] == null)
                {
                    CancelPieceSelection();
                    return;
                }

                // Piece follows cursor while left mouse button is held down
                Vector3 piecePos = new Vector3(mousePos.x, mousePos.y, -1);
                boardUI.Pieces[selectedPieceCoor.SquareIndex()].transform.position = piecePos;
            }
        }

        public void HandlePointAndClick(Vector2 mousePos)
        {
            // New square clicked
            if (Input.GetMouseButtonDown(0))
            {
                Coordinate mouseCoor = MousePositionToCoor(mousePos);

                if (!mouseCoor.IsInBounds())
                    return;

                int targetSquare = mouseCoor.SquareIndex();

                Move chosenMove = ChosenLegalMove(targetSquare);
                
                // Move chosen
                if (!chosenMove.IsNull)
                {
                    SelectedMove = chosenMove;
                    MoveFound = true;
 
                    CancelPieceSelection();
                }
                // Same color piece clicked, select new piece
                else if (Piece.PieceColor(board.Chessboard[targetSquare]) == PlayerColor)
                {
                    if (Piece.IsNone(board.Chessboard[targetSquare]))
                        return;
                    // New piece being selected
                    selectedPieceCoor = mouseCoor;

                     // Stores the possible moves for that selected piece
                    selectedPieceMoves = GenerateSelectedPieceMoves(board, selectedPieceCoor);

                    // Clear previous highlighted moves
                    boardUI.ClearBoard(true, false);
                    boardUI.DrawSquares(true, false);

                    // Highlight the possible moves
                    boardUI.HighlightMoves(board, selectedPieceMoves, selectedPieceCoor);

                    currentInputState = InputState.DraggingPiece;
                }
            }
        }

        public void CancelPieceSelection()
        {
            selectedPieceCoor = null;
            currentInputState = InputState.None;
            selectedPieceMoves = null;
        }

        public Move[] GenerateSelectedPieceMoves(Board board, Coordinate startCoor)
        {
            int startSquare = startCoor.rank * 8 + startCoor.file;

            Span<Move> moves = MoveGenerator.GenerateMoves(startSquare, board, false);
            
            return moves.ToArray();
        }

        public Move ChosenLegalMove(int targetSquare)
        {
            // Moves are generated based on the selected piece
            // If the selected target square is the same as a target square in the list then the move is legal
            
            foreach (Move move in selectedPieceMoves)
            {
                if (move.TargetSquare == targetSquare)
                    return move;
            }
            
            return Move.NullMove;
        }

        public Coordinate GameObjectCoorPosition(GameObject gameObject)
        {   
            return new Coordinate(
                Mathf.FloorToInt(gameObject.transform.localPosition.y / BoardUI.SquareSize), 
                Mathf.FloorToInt(gameObject.transform.localPosition.x / BoardUI.SquareSize)
            );
        }

        public Coordinate MousePositionToCoor(UnityEngine.Vector2 mousePos)
        {
            mousePos = boardUI.SetPositionOrientation(mousePos);

            return new Coordinate(
                Mathf.FloorToInt((float)(mousePos.y + MousePosOffset/ BoardUI.SquareSize)), 
                Mathf.FloorToInt((float)(mousePos.x + MousePosOffset/ BoardUI.SquareSize))
            );
        }
    }
}