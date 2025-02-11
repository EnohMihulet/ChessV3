using System.Collections;
using System.Collections.Generic;
using System;
using Chess.Core;

namespace Chess.Game
{
	public class AIPlayer : Player
	{
		Searcher searcher;
		private bool isSearching = false; // Flag

		public AIPlayer(Board board, int color)
		{
			this.board = board;
			PlayerColor = color;
			OpponentColor = (color == 0) ? 1 : 0;
			PlayerType = GameManager.PlayerType.AI;
			searcher = new Searcher(Searcher.StartDepth, PlayerColor == 0);
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