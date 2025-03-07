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

		public enum PlayerType { Human, AI }
		public enum GameType { PVP, PVB, BVB}

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
		public GameType gameType = GameType.PVP;
		public bool boardUpdated = true;
		public int colorToMove => board.CurrentGameState.ColorToMove ; // 0 white - 1 black
		public PlayerType playerToMove => (colorToMove == 0) ? whitePlayerType: blackPlayerType; // PlayerType
		public int promotionFlag = 0;

		[Header("Interal stuff")]
		public bool gameOver = false;
		public bool HumanPlaysWhite = true;
		public Board board { get; set; }
		public List<string> SANMoves;
		public GameResult.EndResult gameResult => board.CurrentEndResult;
		public BoardUI boardUI;
		public OtherUI otherUI;
		public Evaluation evaluator;
		


		void Start()
		{
			Application.targetFrameRate = 120;

			board = new Board();
			boardUI = FindObjectOfType<BoardUI>();
			otherUI = FindObjectOfType<OtherUI>();

			NewGame();
		}

		void Update()
		{	
			if (gameOver)
				return;

			if (boardUI == null)
				boardUI = FindObjectOfType<BoardUI>();

			if (whitePlayer == null || blackPlayer == null)
				return;

			Player player = colorToMove == 0 ? whitePlayer : blackPlayer;

			// Update game if player selects move
			if (player.MoveFound)
			{
				if (!player.selectedMoveIsPromotion || promotionFlag != 0)
                    UpdateGame();
				else
					otherUI.SetPromotionUIActive();
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

			Move move = player.SelectedMove;

			if (player.selectedMoveIsPromotion)
				move = new Move(move.StartSquare, move.TargetSquare, (uint) promotionFlag);

			SANMoves.Add(MoveHelper.MoveToSANMove(board, move));

			board.MakeMove(move, true);
			boardUpdated = false;

			if (colorToMove == 0)
				otherUI.MakeTurnDisplay();

			if (gameResult != GameResult.EndResult.InProgress)
			{
				gameOver = true;
				GameOver();
			}

			// Updates the display of captured pieces
			if (board.CurrentGameState.CapturedPieceType != 0)
				otherUI.UpdateCapturedPieces(board);

			currentFen = FenUtility.CurrentFen(board);
			zobristDebug = board.CurrentGameState.ZobristHash;

			// Place the pieces in their new location
			boardUI.ClearBoard(true, true);
			boardUI.DrawSquares(HumanPlaysWhite, false);
			boardUI.PlacePieces(board);

			StartCoroutine(otherUI.autoScrollGameHistory());

			// Update player
			player.MoveFound = false;
			player.SelectedMove = null;
			player.StartSearch = false;
			player.selectedMoveIsPromotion = false;
			promotionFlag = 0;

			evaluator = new Evaluation();
			score = evaluator.Evaluate(board);

			boardUpdated = true;
		}

		public void NewGame()
		{
			boardUI.SetPerspective(HumanPlaysWhite);

			// Clear old pieces and squares
			boardUI.ClearBoard(true, true);

			// Clear turn history and past SAN moves
			otherUI.ClearTurnDisplay();
			SANMoves.Clear();

			// Generate the new board
			board = new Board();

			gameOver = false;

			if (gameType == GameType.PVP)
			{
				NewGame(PlayerType.Human, PlayerType.Human);
			}
			else if (gameType == GameType.PVB)
				NewGame(HumanPlaysWhite ? PlayerType.Human : PlayerType.AI, HumanPlaysWhite ? PlayerType.AI : PlayerType.Human);
			else
				NewGame(PlayerType.AI, PlayerType.AI);
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