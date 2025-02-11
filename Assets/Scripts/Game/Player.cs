using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using Chess.Core;
using Unity.VisualScripting;

namespace Chess.Game
{
    public abstract class Player
    {
        public bool MoveFound = false;
        public Move SelectedMove;
        public bool StartSearch = false;
        public Board board;
        public int PlayerColor;
        public int OpponentColor;
        public GameManager.PlayerType PlayerType;

        public abstract void FindMove();
    }
}