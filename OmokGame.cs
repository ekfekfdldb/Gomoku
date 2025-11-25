using System;
using System.Drawing;

namespace Game
{
    public enum Stone
    {
        None = 0,
        Black = 1,
        White = 2
    }

    public class OmokGame
    {
        public const int Size = 19;

        private readonly int[,] _board = new int[Size, Size];
        private readonly int[,] _forbiddenSpots = new int[Size, Size];

        public Stone CurrentTurn { get; private set; } = Stone.Black;
        public bool IsFinished { get; private set; }
        public Stone Winner { get; private set; } = Stone.None;

        public int LastMoveX { get; private set; } = -1;
        public int LastMoveY { get; private set; } = -1;

        public int[,] Board => _board;
        public int[,] ForbiddenSpots => _forbiddenSpots;

        private readonly bool _useAi;
        private readonly OmokAI _ai;

        public OmokGame(bool useAI)
        {
            _useAi = useAI;
            if (_useAi)
            {
                // AI = 백(2), 플레이어 = 흑(1)
                _ai = new OmokAI(_board, (int)Stone.White, (int)Stone.Black);
            }
        }

        public void Reset()
        {
            Array.Clear(_board, 0, _board.Length);
            Array.Clear(_forbiddenSpots, 0, _forbiddenSpots.Length);

            CurrentTurn = Stone.Black;
            IsFinished = false;
            Winner = Stone.None;
            LastMoveX = LastMoveY = -1;

            OmokRules.CalculateForbiddenSpots(_board, _forbiddenSpots);
        }

        public bool PlaceStone(int x, int y)
        {
            if (IsFinished) return false;
            if (x < 0 || x >= Size || y < 0 || y >= Size) return false;
            if (_board[x, y] != 0) return false;

            if (CurrentTurn == Stone.Black)
            {
                int geumsu = OmokRules.CheckForForbiddenMove(_board, x, y);
                if (geumsu != OmokRules.GEUMSU_NONE)
                {
                    // 착수 금지
                    OmokRules.CalculateForbiddenSpots(_board, _forbiddenSpots);
                    return false;
                }
            }

            _board[x, y] = (int)CurrentTurn;
            LastMoveX = x;
            LastMoveY = y;

            if (OmokRules.CheckForWin(x, y, (int)CurrentTurn, _board))
            {
                IsFinished = true;
                Winner = CurrentTurn;
            }

            OmokRules.CalculateForbiddenSpots(_board, _forbiddenSpots);

            if (!IsFinished)
            {
                CurrentTurn = (CurrentTurn == Stone.Black) ? Stone.White : Stone.Black;
            }

            return true;
        }

        public Point? GetAiMove()
        {
            if (!_useAi || _ai == null) return null;
            if (IsFinished) return null;
            if (CurrentTurn != Stone.White) return null;

            var p = _ai.GetNextMove(true);
            if (p.X < 0 || p.Y < 0) return null;
            return p;
        }
    }
}
