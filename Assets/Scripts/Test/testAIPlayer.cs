using System.Collections;
using System.Collections.Generic;
using System;
using Chess.Core;
using Chess.Game;

namespace Chess.Test
{
	public class TestAIPlayer : Player
	{
		OldSearcher searcher;
		private bool isSearching = false; // Flag

		public TestAIPlayer(Board board, int color)
		{
			this.board = board;
			PlayerColor = color;
			OpponentColor = (color == 0) ? 1 : 0;
			PlayerType = GameManager.PlayerType.AI;
			searcher = new OldSearcher(OldSearcher.StartDepth);
		}

		public Move Search()
		{
			Move move = searcher.StartSearch(board);

			return move;
		}

        public override void FindMove()
        {
			if (isSearching)
				return;
			
			isSearching = true;

            Move move = searcher.StartSearch(board);

			isSearching = false;
            SelectedMove = move;
            MoveFound = true;
        }
    }
}