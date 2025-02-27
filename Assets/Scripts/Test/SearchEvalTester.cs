using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Chess.Core;
using Chess.Game;
using TMPro;
using System;

namespace Chess.Test
{
    public class SearchEvalTester : MonoBehaviour
    {
		public bool loadCustomPosition = false;
		public string customPosition;

		public Player whitePlayer;
		public Player blackPlayer;

		public string currentFen;
		// public ulong zobristDebug;
		// public float score;

		public int colorToMove => board.CurrentGameState.ColorToMove ; // 0 white - 1 black

		public Board board { get; set; }
		private GameResult.EndResult gameResult => board.CurrentEndResult;
        public GameObject TestPrefab;
        public GameObject TestParent;

        private int TotalGameCount = 1;
        private int CurrentGameCount = 0;

        private int Win = 0;
        private int Loss = 0;
        private int Draw = 0;

        private bool Stop = false;


		void Start()
		{
			Application.targetFrameRate = 120;

			NewGame();
		}

		void Update()
		{	
            if (Stop)
            {
                return;
            }

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

			Move move = player.SelectedMove;

			board.MakeMove(move, true);

            Debug.Log(board.CurrentGameState.FullmoveCount);


			if (gameResult != GameResult.EndResult.InProgress)
            {
                GameOver();
                Debug.Log(FenUtility.CurrentFen(board));

                CurrentGameCount++;

                if (gameResult == GameResult.EndResult.WhiteIsMated)
                    Loss++;
                else if (gameResult == GameResult.EndResult.BlackIsMated)
                    Win++;
                else
                    Draw++;


                if (CurrentGameCount < TotalGameCount)
                    NewGame();
                else
                {
                    Stop = true;
                    NewGamePanel();
                }
            }

			// currentFen = FenUtility.CurrentFen(board);
			// zobristDebug = board.CurrentGameState.ZobristHash;

			// Update player
			player.MoveFound = false;
			player.SelectedMove = null;
			player.StartSearch = false;
			player.selectedMoveIsPromotion = false;

			// evaluator = new Evaluation();
			// score = evaluator.Evaluate(board);
		}

		public void NewGame()
		{
			// Generate the new board
			board = new Board();

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

            whitePlayer = new AIPlayer(board, 0);
            blackPlayer = new TestAIPlayer(board, 1);
		}

		public void QuitGame()
		{
			Application.Quit();
		}

		void GameOver()
		{
			Debug.Log("Game Over " + gameResult);
		}

        void NewGamePanel()
        {
            GameObject test = Instantiate(TestPrefab, new Vector3(0, 200, 0), Quaternion.identity);
                    
            test.transform.SetParent(TestParent.transform, false);

            TMP_Text[] testTexts = test.GetComponentsInChildren<TMP_Text>();

            testTexts[1].text = Win.ToString();
            testTexts[3].text = Loss.ToString();
            testTexts[5].text = Draw.ToString();
            testTexts[7].text = TotalGameCount.ToString();
            testTexts[9].text = (Win/TotalGameCount * 100).ToString() + "%";
        }
    }
}

