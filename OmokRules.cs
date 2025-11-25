using System;

namespace Game
{
    public static class OmokRules
    {
        private const int BOARD_SIZE = 19;

        public const int GEUMSU_NONE = 0;
        public const int GEUMSU_SANGSAM = 1;
        public const int GEUMSU_SANGSA = 2;
        public const int GEUMSU_JANGMOK = 3;

        private static int[] loc = new int[3];
        private static int[] inner_blank = new int[2];
        private static int[] s_inner_blank = new int[2];
        private static int stone;
        private static int blank;

        private static int CurX;
        private static int CurY;

        private static readonly int[,] Dir = new int[,] {
            { 0, 1 },
            { 1, 0 },
            { 1, 1 },
            { 1, -1 }
        };

        private static bool CheckBoundary(int x, int y)
        {
            return y < 0 || y >= BOARD_SIZE || x < 0 || x >= BOARD_SIZE;
        }

        public static bool CheckForWin(int x, int y, int player, int[,] board)
        {
            for (int i = 0; i < 4; i++)
            {
                int dx = Dir[i, 1];
                int dy = Dir[i, 0];
                int count = 1;
                count += CountConsecutive(x, y, dx, dy, player, board);
                count += CountConsecutive(x, y, -dx, -dy, player, board);
                if (count == 5) return true;
            }
            return false;
        }

        private static int CountConsecutive(int x, int y, int dx, int dy, int player, int[,] board)
        {
            int count = 0;
            for (int i = 1; i < 6; i++)
            {
                int nx = x + dx * i;
                int ny = y + dy * i;
                if (CheckBoundary(nx, ny) || board[nx, ny] != player) break;
                count++;
            }
            return count;
        }

        public static int CheckForForbiddenMove(int[,] boardState, int x, int y)
        {
            CurX = x;
            CurY = y;

            boardState[x, y] = 1;

            if (CheckJangmok(boardState, x, y))
            {
                boardState[x, y] = 0;
                return GEUMSU_JANGMOK;
            }

            int open_three = 0;
            int open_four = 0;

            for (int i = 0; i < 4; i++)
            {
                int temp = FindOpen(i, boardState);

                if (temp == 1)
                {
                    open_three++;
                }
                else if (temp == 2)
                {
                    open_four++;
                }
                else if (temp == 3)
                {
                    boardState[x, y] = 0;
                    return GEUMSU_SANGSA;
                }
            }

            boardState[x, y] = 0;

            if (open_three >= 2) return GEUMSU_SANGSAM;
            if (open_four >= 2) return GEUMSU_SANGSA;

            return GEUMSU_NONE;
        }

        private static bool CheckJangmok(int[,] board, int x, int y)
        {
            for (int i = 0; i < 4; i++)
            {
                int dx = Dir[i, 1];
                int dy = Dir[i, 0];
                int count = 1;
                count += CountConsecutive(x, y, dx, dy, 1, board);
                count += CountConsecutive(x, y, -dx, -dy, 1, board);
                if (count >= 6) return true;
            }
            return false;
        }

        private static int FindOpen(int d, int[,] board)
        {
            int stone1, stone2;
            int allStone = 0;

            int x = Dir[d, 1];
            int y = Dir[d, 0];

            int blank1, blank2;

            loc[0] = 0; loc[1] = 0; loc[2] = 0;
            inner_blank[0] = -1; inner_blank[1] = -1;
            s_inner_blank[0] = -1; s_inner_blank[1] = -1;
            blank = 0;

            int end1, end2;

            end1 = SearchSide(x, y, board);
            stone1 = stone;
            blank1 = blank;
            blank = (end1 == 2) ? 1 : 0;

            end2 = SearchSide(x * -1, y * -1, board);
            stone2 = stone;
            blank2 = blank;

            if (stone2 == 2) { loc[0] *= -1; loc[1] *= -1; }
            else if (stone2 == 1) { loc[1] *= -1; }

            allStone = stone1 + stone2;

            if (allStone == 2)
            {
                if (blank1 == 1 && blank2 == 1)
                {
                    if (((end1 == 2 ? 1 : 0) + (end2 == 2 ? 1 : 0)) <= 1 || end1 == 0 || end2 == 0)
                    {
                        int left = loc[0];
                        int right = loc[1];

                        if (stone1 == 2) { left = 0; right = loc[1]; }
                        else if (stone2 == 2) { left = loc[1]; right = 0; }

                        if (Can6(left, right, d, end1, end2, blank1, blank2, 0, board) == 1)
                            return 0;

                        return 1;
                    }
                }
            }
            else if (allStone == 3)
            {
                int left = loc[2];
                int right = loc[2];

                switch (stone1)
                {
                    case 0: right = 0; break;
                    case 1: left = loc[0]; break;
                    case 2: left = loc[1]; break;
                    case 3: left = 0; break;
                }

                if (Can6(left, right, d, end1, end2, blank1, blank2, 1, board) == 0)
                {
                    if (((end1 == 2 ? 1 : 0) + (end2 == 2 ? 1 : 0)) == 1) return 2;
                    if (blank1 == 1 || blank2 == 1) return 2;
                }
            }

            if (s_inner_blank[0] != -1 && inner_blank[0] != -1 && (allStone == 4 || allStone == 5))
            {
                if (allStone == 4)
                {
                    int dist = (inner_blank[0] - s_inner_blank[0] == 0)
                        ? (inner_blank[1] - s_inner_blank[1])
                        : (inner_blank[0] - s_inner_blank[0]);

                    dist = Math.Abs(dist);
                    if (dist == 4) return 3;
                }
                else if (allStone == 5)
                {
                    int innerX1 = inner_blank[1] + 2 * x;
                    int innerY1 = inner_blank[0] + 2 * y;
                    int innerX2 = inner_blank[1] - 2 * x;
                    int innerY2 = inner_blank[0] - 2 * y;

                    if (!CheckBoundary(innerX1, innerY1) && !CheckBoundary(innerX2, innerY2))
                    {
                        if (board[innerX1, innerY1] == 1 && board[innerX2, innerY2] == 1)
                        {
                            return 3;
                        }
                    }
                }
            }
            return 0;
        }

        private static int SearchSide(int x, int y, int[,] board)
        {
            int xx = CurX + x;
            int yy = CurY + y;
            int check = 0;
            stone = 0;
            int blank_black = 0;

            while (true)
            {
                if (CheckBoundary(xx, yy) || board[xx, yy] == 2)
                    return 1 + blank_black;

                if (board[xx, yy] == 1)
                {
                    if (check == 1)
                    {
                        blank_black++;
                        if (inner_blank[0] == -1) { inner_blank[0] = yy - y; inner_blank[1] = xx - x; }
                        else if (s_inner_blank[0] == -1) { s_inner_blank[0] = yy - y; s_inner_blank[1] = xx - x; }
                    }
                    check = 0;
                    blank = 0;
                    stone++;

                    int dist = (x != 0) ? (xx - CurX) * x : (yy - CurY) * y;

                    if (loc[0] == 0) loc[0] = dist;
                    else if (loc[1] == 0) loc[1] = dist;
                    else if (loc[2] == 0) loc[2] = dist;
                }

                if (board[xx, yy] == 0)
                {
                    if (check == 0 && blank == 0)
                    {
                        check = 1;
                        blank = 1;
                    }
                    else
                    {
                        return 0;
                    }
                }

                xx += x;
                yy += y;
            }
        }

        private static int Can6(int left, int right, int d, int end1, int end2, int blank1, int blank2, int threefour, int[,] board)
        {
            int x = Dir[d, 1];
            int y = Dir[d, 0];

            int leftX = CurX + 5 * x + left;
            int leftY = CurY + 5 * y + left;

            int rightX = CurX + 5 * (-x) + right;
            int rightY = CurY + 5 * (-y) + right;

            bool is6left = (!CheckBoundary(leftX, leftY) && board[leftX, leftY] == 1);
            bool is6right = (!CheckBoundary(rightX, rightY) && board[rightX, rightY] == 1);

            bool check = false;

            if (threefour == 0)
            {
                if ((is6left && end2 == 1) || (is6right && end1 == 1)) check = true;
            }
            if (threefour == 1)
            {
                if ((is6left && blank2 == 0 && end2 == 1) || (is6right && blank1 == 0 && end1 == 1)) check = true;
            }

            if (check || (is6left && is6right)) return 1;
            return 0;
        }

        public static void CalculateForbiddenSpots(int[,] boardState, int[,] forbiddenSpots)
        {
            Array.Clear(forbiddenSpots, 0, forbiddenSpots.Length);

            for (int x = 0; x < BOARD_SIZE; x++)
            {
                for (int y = 0; y < BOARD_SIZE; y++)
                {
                    if (boardState[x, y] == 0)
                    {
                        forbiddenSpots[x, y] = CheckForForbiddenMove(boardState, x, y);
                    }
                    else
                    {
                        forbiddenSpots[x, y] = GEUMSU_NONE;
                    }
                }
            }
        }
    }
}
