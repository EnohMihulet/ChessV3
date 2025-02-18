using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Chess.Core;
using System;
using UnityEditor.Experimental.GraphView;
using Unity.VisualScripting;
using Unity.Mathematics;

namespace Chess.Game
{
	public class BoardUI : MonoBehaviour
	{
        [Header("Squares")]
        public GameObject SquaresParent;
        public GameObject SquarePrefab;
        public GameObject[] Squares = new GameObject[Board.Squares];

        [Header("Pieces")]
        public GameObject PiecesParent;
        public static GameObject WPawn, WKnight, WBishop, WRook, WQueen, WKing, BPawn, BBishop, BKnight, BRook, BQueen, BKing;
        public GameObject[] PiecePrefabs = new GameObject[15] 
        {
            null, WPawn, WKnight, WBishop, WRook, WQueen, WKing, null, null, BPawn, BBishop, BKnight, BRook, BQueen, BKing
        };
        public GameObject[] Pieces = new GameObject[Board.Squares];

        [Header("Board Labels")]
        public GameObject WhitesideBoardLabels;
        public GameObject BlacksideBoardLabels;

        [Header("Square Generation")]
        public int Orientation = 0; // Stores orientation of the board (0 is white at the bottom)
        public const int SquareSize = 1;
        public Color LightSquareColor = new Color(235/255f, 210/255f, 171/255f, 1.0f); // light square color (beige)
        public Color DarkSquareColor = new Color(31/255f, 84/255f, 42/255f, 1.0f); // Dark square color (green)
        public Color HighlightColor = new Color(1.0f, .0431f, .3528f, .5f); // Highlight Color (pink)
        public Color SelectedPieceColor = new Color(1.0f, .0431f, .3528f, 1.0f); // Selected piece color (pink)

        void Start()
        {
            Camera.main.transform.position = new Vector3(5, 3.5f, -10);
            Camera.main.transform.LookAt(new Vector3(5, 3.5f, 0));
        }
        
        public void DrawSquares(bool humanPlaysWhite, bool changeLabels = true)
        {
            for (int rank = 0; rank < Board.ranks; rank++) 
            {
                for (int file = 0; file < Board.files; file++) 
                {
                    // Square positions
                    int xPosition = file * SquareSize;
                    int yPosition = rank * SquareSize;

                    GameObject square = Instantiate(SquarePrefab, new Vector3(xPosition, yPosition, 0), Quaternion.identity);
                    
                    // Set the square's parent to Squares
                    square.transform.SetParent(SquaresParent.transform, false);

                    Renderer renderer = square.GetComponent<Renderer>();

                    // Alternate colors for the chessboard pattern
                    renderer.material.color = LightSquareColor;

                    if ((rank + file) % 2 == 0) 
                        renderer.material.color = DarkSquareColor;

                    Squares[rank * 8 + file] = square;
                }

                if (changeLabels)
                {
                    WhitesideBoardLabels.SetActive(humanPlaysWhite);
                    BlacksideBoardLabels.SetActive(!humanPlaysWhite);
                }
            }
        }

        public void PlacePieces(Board board)
        {
            // Sets the rotation of the pieces
            quaternion rotation =  new Quaternion(0,0,0,0);
            if (Orientation == 1)
                rotation = new Quaternion(0,0,180,0);

            // Places the pieces
            for (int rank = 0; rank < Board.ranks; rank++) 
            {
                for (int file = 0; file < Board.files; file++) 
                {   
                    // Piece positions
                    int xPosition = file * SquareSize;
                    int yPosition = rank * SquareSize;
                    int squareIndex = rank * 8 + file;

                    int piece = board.Chessboard[squareIndex];

                    if (piece == Piece.None) // Empty square
                        continue;

                    GameObject piecePrefab = PiecePrefabs[piece];

                    GameObject placedPiece = Instantiate(piecePrefab, new Vector3(xPosition, yPosition, -1), rotation);
                    
                    // Set the piece's parent to Pieces
                    placedPiece.transform.SetParent(PiecesParent.transform, false);

                    Pieces[squareIndex] = placedPiece;
                }
            }
            // Changes the color that faces the player
            transform.rotation = rotation;
            transform.position = new Vector3(0,0,0);
            
            if (Orientation == 1)
                transform.position = new Vector3(7,7,0);
        }

        public void ClearBoard(bool clearSquares, bool clearPieces) {
            // Destroy all squares
            if (clearSquares) {
                foreach (GameObject square in Squares)
                {
                    Destroy(square);
                }
            }
        
            // Destroy all pieces
            if (clearPieces) {
                foreach (GameObject piece in Pieces)
                {
                    Destroy(piece);
                }
            }
        }

        public void SetPerspective(bool humanPlaysWhite)
        {
            Orientation = humanPlaysWhite ? 0 : 1;
        }

        public Vector2 SetPositionOrientation(Vector2 pos)
        {
            if (Orientation == 1)
                return new Vector2(7 - pos.x, 7 - pos.y);
            else 
                return pos;
        }

        public void HighlightMoves(Board board, Move[] moves, Coordinate selectedPieceCoor)
        {
            foreach (Move move in moves)
            {
                int targetSquare = move.TargetSquare;

                GameObject square = Squares[targetSquare];

                // Change color of squares that are possible moves
                Renderer renderer = square.GetComponent<Renderer>();
                renderer.material.color = HighlightColor;
            }

            GameObject selectedSquare = Squares[selectedPieceCoor.rank * 8 + selectedPieceCoor.file];
            Renderer renderer2 = selectedSquare.GetComponent<Renderer>();
            renderer2.material.color = SelectedPieceColor;
        }
    }
}