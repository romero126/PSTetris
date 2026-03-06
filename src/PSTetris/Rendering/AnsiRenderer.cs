using System;
using System.Text;

namespace PSTetris.Rendering
{
    public class AnsiRenderer : IGameRenderer
    {
        private int _boardWidth;
        private int _boardHeight;
        private int _infoCol;

        public void Initialize(int boardWidth, int boardHeight)
        {
            _boardWidth = boardWidth;
            _boardHeight = boardHeight;
            _infoCol = boardWidth * 2 + 3;

            Console.CursorVisible = false;
            Console.Clear();
            DrawBorder();
        }

        public void RenderFrame(GameState state)
        {
            var sb = new StringBuilder(_boardWidth * 4 + 4);
            for (int r = 0; r < _boardHeight; r++)
            {
                Console.SetCursorPosition(1, r + 1);
                sb.Clear();
                for (int c = 0; c < _boardWidth; c++)
                {
                    int v = state.Display[r, c];
                    if (v > 0)
                        sb.Append(TetrisColors.AnsiColors[v]).Append("\u2588\u2588");
                    else if (v == -1)
                        sb.Append(TetrisColors.AnsiGhost).Append("\u2591\u2591");
                    else
                        sb.Append(TetrisColors.AnsiReset).Append("  ");
                }
                sb.Append(TetrisColors.AnsiReset);
                Console.Write(sb);
            }
        }

        public void RenderInfo(GameState state)
        {
            void At(int row, string text)
            {
                Console.SetCursorPosition(_infoCol, row);
                Console.Write(TetrisColors.AnsiReset + text.PadRight(16));
            }

            At(0,  "\u250c\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2510");
            At(1,  "\u2502  SCORE       \u2502");
            At(2,  "\u2502  " + state.Score.ToString().PadRight(12) + "\u2502");
            At(3,  "\u2502              \u2502");
            At(4,  "\u2502  LEVEL       \u2502");
            At(5,  "\u2502  " + state.Level.ToString().PadRight(12) + "\u2502");
            At(6,  "\u2502              \u2502");
            At(7,  "\u2502  LINES       \u2502");
            At(8,  "\u2502  " + state.Lines.ToString().PadRight(12) + "\u2502");
            At(9,  "\u2502              \u2502");
            At(10, "\u2502  NEXT        \u2502");

            for (int r = 0; r < 4; r++)
            {
                Console.SetCursorPosition(_infoCol, 11 + r);
                Console.Write(TetrisColors.AnsiReset + "\u2502              \u2502");
            }

            if (state.NextPiece != null)
            {
                foreach (var cell in state.NextPiece.GetCells())
                {
                    int r = cell[0], c = cell[1];
                    if (r >= 0 && r < 4 && c >= 0 && c < 4)
                    {
                        Console.SetCursorPosition(_infoCol + 2 + c * 2, 11 + r);
                        Console.Write(TetrisColors.AnsiColors[(int)state.NextPiece.Type]
                                      + "\u2588\u2588" + TetrisColors.AnsiReset);
                    }
                }
            }

            At(15, "\u2514\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2518");
            At(16, "");
            At(17, " \u2190\u2192/AD  Move");
            At(18, " \u2191/W    Rotate CW");
            At(19, " Z/X    Rotate CCW");
            At(20, " \u2193/S    Soft drop");
            At(21, " Spc    Hard drop");
            At(22, " P      Pause");
            At(23, " Esc/Q  Quit");

            if (state.Paused)
            {
                Console.SetCursorPosition(3, _boardHeight / 2);
                Console.Write("\x1b[97;44m   ** PAUSED **   " + TetrisColors.AnsiReset);
            }
        }

        public bool RenderGameOver(GameState state)
        {
            string msg1 = "  GAME OVER   ";
            string msg2 = string.Format("  Score: {0,-7}", state.Score);
            string msg3 = "  R: Restart  ";
            string msg4 = "  Q: Quit     ";
            int midRow = _boardHeight / 2 - 2;
            int maxLen = msg1.Length;
            int startCol = (_boardWidth * 2 - maxLen) / 2 + 1;

            Console.SetCursorPosition(startCol, midRow);
            Console.Write("\x1b[97;41m" + msg1.PadRight(maxLen) + TetrisColors.AnsiReset);
            Console.SetCursorPosition(startCol, midRow + 1);
            Console.Write("\x1b[97;41m" + msg2.PadRight(maxLen) + TetrisColors.AnsiReset);
            Console.SetCursorPosition(startCol, midRow + 2);
            Console.Write("\x1b[97;41m" + "".PadRight(maxLen) + TetrisColors.AnsiReset);
            Console.SetCursorPosition(startCol, midRow + 3);
            Console.Write("\x1b[97;41m" + msg3.PadRight(maxLen) + TetrisColors.AnsiReset);
            Console.SetCursorPosition(startCol, midRow + 4);
            Console.Write("\x1b[97;41m" + msg4.PadRight(maxLen) + TetrisColors.AnsiReset);

            while (Console.KeyAvailable) Console.ReadKey(intercept: true);
            while (true)
            {
                var key = Console.ReadKey(intercept: true).Key;
                if (key == ConsoleKey.R) return true;
                if (key == ConsoleKey.Q || key == ConsoleKey.Escape) return false;
            }
        }

        public void Cleanup()
        {
            Console.SetCursorPosition(0, _boardHeight + 3);
            Console.Write(TetrisColors.AnsiReset);
            Console.CursorVisible = true;
        }

        private void DrawBorder()
        {
            Console.Write(TetrisColors.AnsiBorder);

            Console.SetCursorPosition(0, 0);
            Console.Write("\u2554" + new string('\u2550', _boardWidth * 2) + "\u2557");

            for (int r = 0; r < _boardHeight; r++)
            {
                Console.SetCursorPosition(0, r + 1);
                Console.Write("\u2551");
                Console.SetCursorPosition(_boardWidth * 2 + 1, r + 1);
                Console.Write("\u2551");
            }

            Console.SetCursorPosition(0, _boardHeight + 1);
            Console.Write("\u255a" + new string('\u2550', _boardWidth * 2) + "\u255d");

            Console.Write(TetrisColors.AnsiReset);
        }
    }
}
