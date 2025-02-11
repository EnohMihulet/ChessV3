using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Chess.Core;
using Unity.VisualScripting;

namespace Chess.Game
{
	public class GameManager : MonoBehaviour
	{

		public event System.Action onPositionLoaded;
		public enum PlayerType { Human, AI }

		[Header("Start Position")]
		public bool loadCustomPosition = false;
		public string customPosition;

		[Header("Players")]
		public PlayerType whitePlayerType;
		public PlayerType blackPlayerType;
		public Player whitePlayer;
		public Player blackPlayer;

		[Header("Debug")]
		public string currentFen;
		public ulong zobristDebug;
		public float score;

		[Header("Game State")]
		public GameResult.EndResult gameResult;
		public bool boardUpdated = true;
		public int colorToMove => board.CurrentGameState.ColorToMove ; // 0 white - 1 black
		public PlayerType playerToMove => (colorToMove == 0) ? whitePlayerType: blackPlayerType; // PlayerType

		[Header("Interal stuff")]
		public bool HumanPlaysWhite = true;
		public bool VerseAI = false;
		public Board board { get; set; }
		public BoardUI boardUI;
		public OtherUI otherUI;
		


		void Start()
		{
			Application.targetFrameRate = 120;

			board = new Board();
			boardUI = FindObjectOfType<BoardUI>();
			otherUI = FindObjectOfType<OtherUI>();

			NewGame(HumanPlaysWhite);
		}

		void Update()
		{	
			if (boardUI == null)
				boardUI = FindObjectOfType<BoardUI>();

			if (whitePlayer == null || blackPlayer == null)
				return;

			Player player = colorToMove == 0 ? whitePlayer : blackPlayer;

			// Update game if player selects move
			if (player.MoveFound)
			{
				UpdateGame();
			}
			else
			{
				player.FindMove();
			}
		}
		
		void UpdateGame()
		{	
			Player player = (colorToMove == 0) ? whitePlayer : blackPlayer;
			Player opponent = (colorToMove == 0) ? blackPlayer : whitePlayer;
			
			board.MakeMove(player.SelectedMove, true);
			boardUpdated = false;			

			// Updates the display of captured pieces
			if (board.CurrentGameState.CapturedPieceType != 0)
				otherUI.UpdateCapturedPieces(board);

			currentFen = FenUtility.CurrentFen(board);
			zobristDebug = board.CurrentGameState.ZobristHash;

			// Place the pieces in their new location
			boardUI.ClearBoard(true, true);
			boardUI.DrawSquares(HumanPlaysWhite);
			boardUI.PlacePieces(board);

			// Update player
			player.MoveFound = false;
			player.SelectedMove = null;
			player.StartSearch = false;

			score = Evaluation.Evaluate(board);

			boardUpdated = true;
		}

		public void NewGame(bool humanPlaysWhite)
		{
			boardUI.SetPerspective(humanPlaysWhite);

			// Clear old pieces and squares
			boardUI.ClearBoard(true, true);

			// Generate the new board
			board = new Board();

			if (VerseAI)
				NewGame(humanPlaysWhite ? PlayerType.Human : PlayerType.AI, humanPlaysWhite ? PlayerType.AI : PlayerType.Human);
			else
				NewGame(PlayerType.Human, PlayerType.Human);
		}

		void NewGame(PlayerType whitePlayerType, PlayerType blackPlayerType)
		{
			if (loadCustomPosition)
			{
				currentFen = customPosition;
				board.LoadPosition(customPosition);
			}
			else
			{
				currentFen = FenUtility.StartPositionFEN;
				board.LoadStartPosition();
			}

			onPositionLoaded?.Invoke();
			boardUI.DrawSquares(HumanPlaysWhite);
			boardUI.PlacePieces(board);

			this.whitePlayerType = whitePlayerType;
			this.blackPlayerType = blackPlayerType;

			if (whitePlayerType == PlayerType.Human)
				whitePlayer = new HumanPlayer(board, 0);

			if (whitePlayerType == PlayerType.AI)
				whitePlayer = new AIPlayer(board, 0);

			if (blackPlayerType == PlayerType.Human)
				blackPlayer = new HumanPlayer(board, 1);
			
			if (blackPlayerType == PlayerType.AI)
				blackPlayer = new AIPlayer(board, 1);

			gameResult = GameResult.EndResult.InProgress;
		}

		public void QuitGame()
		{
			Application.Quit();
		}

		void GameOver()
		{
			Debug.Log("Game Over " + gameResult);
		}
	}
}