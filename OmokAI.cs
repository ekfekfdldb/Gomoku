using System;
using System.Collections.Generic;
using System.Drawing;

namespace Game
{
    public class OmokAI
    {
        private readonly int[,] boardState;
        private readonly int size;

        private int aiTurn;
        private int playerTurn;
        private readonly int searchRange = 3;

        private static readonly Random rand = new Random();

        private static readonly (int dx, int dy)[] dirs =
        {
            (1, 0),
            (0, 1),
            (1, 1),
            (1, -1)
        };

        public OmokAI(int[,] boardState, int aiTurn, int playerTurn)
        {
            this.boardState = boardState;
            this.size = boardState.GetLength(0);
            this.aiTurn = aiTurn;
            this.playerTurn = playerTurn;
        }

        public void UpdatePlayers(int aiTurn, int playerTurn)
        {
            this.aiTurn = aiTurn;
            this.playerTurn = playerTurn;
        }

        private bool IsWinningMove(int x, int y, int player)
        {
            foreach (var (dx, dy) in dirs)
            {
                int count = CountSequence(x, y, player, dx, dy);
                if (count >= 5) return true;
            }
            return false;
        }

        private int GetMaxSequenceLength(int x, int y, int player)
        {
            int best = 0;
            foreach (var (dx, dy) in dirs)
            {
                int count = CountSequence(x, y, player, dx, dy);
                if (count > best) best = count;
            }
            return best;
        }

        private int CountSequence(int x, int y, int player, int dx, int dy)
        {
            int count = 1;

            int nx = x + dx;
            int ny = y + dy;
            while (IsOnBoard(nx, ny) && boardState[nx, ny] == player)
            {
                count++;
                nx += dx;
                ny += dy;
            }

            nx = x - dx;
            ny = y - dy;
            while (IsOnBoard(nx, ny) && boardState[nx, ny] == player)
            {
                count++;
                nx -= dx;
                ny -= dy;
            }

            return count;
        }

        public Point GetNextMove(bool useMinimax = true)
        {
            var moves = FindMoves();

            if (moves.Count == 0)
            {
                int mid = size / 2;
                return new Point(mid, mid);
            }

            foreach (var p in moves)
            {
                boardState[p.X, p.Y] = aiTurn;
                bool win = IsWinningMove(p.X, p.Y, aiTurn);
                boardState[p.X, p.Y] = 0;
                if (win) return p;
            }

            foreach (var p in moves)
            {
                boardState[p.X, p.Y] = playerTurn;
                bool win = IsWinningMove(p.X, p.Y, playerTurn);
                boardState[p.X, p.Y] = 0;
                if (win) return p;
            }

            foreach (var p in moves)
            {
                boardState[p.X, p.Y] = aiTurn;
                int len = GetMaxSequenceLength(p.X, p.Y, aiTurn);
                boardState[p.X, p.Y] = 0;
                if (len >= 4) return p;
            }

            foreach (var p in moves)
            {
                boardState[p.X, p.Y] = playerTurn;
                int len = GetMaxSequenceLength(p.X, p.Y, playerTurn);
                boardState[p.X, p.Y] = 0;
                if (len >= 4) return p;
            }

            return (!useMinimax) ? GetFastMove(moves) : GetSafeMove(moves);
        }

        private Point GetFastMove(List<Point> moves)
        {
            int bestScore = int.MinValue;
            Point bestMove = new Point(-1, -1);

            foreach (var p in moves)
            {
                int attackScore = EvaluatePosition(p.X, p.Y, aiTurn);
                int defenseScore = EvaluatePosition(p.X, p.Y, playerTurn);

                int totalScore = attackScore * 2 + defenseScore * 3 + rand.Next(0, 4);

                if (totalScore > bestScore)
                {
                    bestScore = totalScore;
                    bestMove = p;
                }
            }

            if (bestMove.X < 0 && bestMove.Y < 0)
                return moves[rand.Next(moves.Count)];

            return bestMove;
        }

        private Point GetSafeMove(List<Point> moves)
        {
            var scored = new List<(Point p, int score)>();
            foreach (var p in moves)
            {
                int s = EvaluatePosition(p.X, p.Y, aiTurn)
                      + EvaluatePosition(p.X, p.Y, playerTurn);
                scored.Add((p, s));
            }

            scored.Sort((a, b) => b.score.CompareTo(a.score));

            int limit = Math.Min(10, scored.Count);
            var limitedMoves = new List<Point>();
            for (int i = 0; i < limit; i++)
                limitedMoves.Add(scored[i].p);

            int bestScore = int.MinValue;
            Point bestMove = new Point(-1, -1);

            foreach (var aiMove in limitedMoves)
            {
                boardState[aiMove.X, aiMove.Y] = aiTurn;

                var replyMoves = FindMoves();
                int worstScore = int.MaxValue;

                foreach (var hMove in replyMoves)
                {
                    boardState[hMove.X, hMove.Y] = playerTurn;
                    int score = EvaluateBoard();
                    if (score < worstScore) worstScore = score;
                    boardState[hMove.X, hMove.Y] = 0;
                }

                boardState[aiMove.X, aiMove.Y] = 0;

                if (worstScore > bestScore)
                {
                    bestScore = worstScore;
                    bestMove = aiMove;
                }
            }

            if (bestMove.X < 0 && bestMove.Y < 0)
                return moves[rand.Next(moves.Count)];

            return bestMove;
        }

        private List<Point> FindMoves()
        {
            var moves = new List<Point>();

            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                {
                    if (boardState[x, y] != 0) continue;
                    if (!HasNeighbor(x, y, searchRange)) continue;

                    if (aiTurn == 1)
                    {
                        int r = OmokRules.CheckForForbiddenMove(boardState, x, y);
                        if (r != OmokRules.GEUMSU_NONE)
                            continue;
                    }

                    moves.Add(new Point(x, y));
                }

            return moves;
        }

        private bool HasNeighbor(int x, int y, int dist)
        {
            for (int dx = -dist; dx <= dist; dx++)
                for (int dy = -dist; dy <= dist; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = x + dx;
                    int ny = y + dy;
                    if (IsOnBoard(nx, ny) && boardState[nx, ny] != 0)
                        return true;
                }

            return false;
        }

        private int EvaluateBoard()
        {
            int score = 0;

            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                {
                    if (boardState[x, y] == aiTurn)
                        score += EvaluatePosition(x, y, aiTurn);
                    else if (boardState[x, y] == playerTurn)
                        score -= EvaluatePosition(x, y, playerTurn);
                }

            return score;
        }

        private int EvaluatePosition(int x, int y, int player)
        {
            int s = 0;

            s += EvaluateDirection(x, y, player, 1, 0);
            s += EvaluateDirection(x, y, player, 0, 1);
            s += EvaluateDirection(x, y, player, 1, 1);
            s += EvaluateDirection(x, y, player, 1, -1);

            int center = size / 2;
            int dist = Math.Abs(x - center) + Math.Abs(y - center);
            s += Math.Max(0, 16 - dist);

            return s;
        }

        private int EvaluateDirection(int x, int y, int player, int dx, int dy)
        {
            int count = 1;
            bool blocked1 = false, blocked2 = false;

            int nx = x + dx, ny = y + dy;
            while (IsOnBoard(nx, ny) && boardState[nx, ny] == player)
            {
                count++; nx += dx; ny += dy;
            }
            if (!IsOnBoard(nx, ny) || boardState[nx, ny] != 0)
                blocked1 = true;

            nx = x - dx; ny = y - dy;
            while (IsOnBoard(nx, ny) && boardState[nx, ny] == player)
            {
                count++; nx -= dx; ny -= dy;
            }
            if (!IsOnBoard(nx, ny) || boardState[nx, ny] != 0)
                blocked2 = true;

            int baseScore = ScoreSequence(count);

            if (blocked1 && blocked2) baseScore /= 3;
            else if (blocked1 || blocked2) baseScore = baseScore * 2 / 3;

            return baseScore;
        }

        private bool IsOnBoard(int x, int y)
        {
            return x >= 0 && x < size && y >= 0 && y < size;
        }

        private int ScoreSequence(int cnt)
        {
            if (cnt >= 5) return 100000;
            if (cnt == 4) return 15000;
            if (cnt == 3) return 1200;
            if (cnt == 2) return 80;
            return 0;
        }
    }
}
