using System;
using System.Text;

namespace PSTetris.Rendering
{
    public class AnsiRenderer : IGameRenderer
    {
        private int _boardWidth;
        private int _boardHeight;
        private int _scale = 1;
        private int _infoCol;
        private bool _altScreenActive;

        public void Initialize(int boardWidth, int boardHeight)
        {
            _boardWidth = boardWidth;
            _boardHeight = boardHeight;

            // Compute scale from available terminal size
            int availCols = Console.WindowWidth;
            int availRows = Console.WindowHeight;

            // Info panel needs ~18 columns; border = 2; gap = 1
            int maxScaleW = (availCols - 2 - 1 - 18) / (boardWidth * 2);
            int maxScaleH = (availRows - 2) / boardHeight;
            _scale = Math.Max(1, Math.Min(maxScaleW, maxScaleH));

            _infoCol = boardWidth * 2 * _scale + 3;

            // Switch to alternate screen buffer on first init
            if (!_altScreenActive)
            {
                Console.Write("\x1b[?1049h");
                _altScreenActive = true;
            }

            Console.CursorVisible = false;
            Console.Clear();
            DrawBorder();
        }

        public void RenderFrame(GameState state)
        {
            var sb = new StringBuilder(_boardWidth * 2 * _scale + 4);
            for (int r = 0; r < _boardHeight; r++)
            {
                for (int sy = 0; sy < _scale; sy++)
                {
                    Console.SetCursorPosition(1, r * _scale + sy + 1);
                    sb.Clear();
                    for (int c = 0; c < _boardWidth; c++)
                    {
                        int v = state.Display[r, c];
                        if (v > 0)
                            sb.Append(TetrisColors.AnsiColors[v])
                              .Append(new string('\u2588', 2 * _scale));
                        else if (v == -1)
                            sb.Append(TetrisColors.AnsiGhost)
                              .Append(new string('\u2591', 2 * _scale));
                        else
                            sb.Append(TetrisColors.AnsiReset)
                              .Append(new string(' ', 2 * _scale));
                    }
                    sb.Append(TetrisColors.AnsiReset);
                    Console.Write(sb);
                }
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
                int midCol = 1 + (_boardWidth * _scale - 9);
                int midRow = _boardHeight * _scale / 2;
                Console.SetCursorPosition(Math.Max(1, midCol), midRow + 1);
                Console.Write("\x1b[97;44m   ** PAUSED **   " + TetrisColors.AnsiReset);
            }
        }

        public bool RenderGameOver(GameState state)
        {
            string msg1 = "  GAME OVER   ";
            string msg2 = string.Format("  Score: {0,-7}", state.Score);
            string msg3 = "  R: Restart  ";
            string msg4 = "  Q: Quit     ";
            int boardRows = _boardHeight * _scale;
            int boardCols = _boardWidth * 2 * _scale;
            int midRow = boardRows / 2 - 2 + 1;
            int maxLen = msg1.Length;
            int startCol = (boardCols - maxLen) / 2 + 1;

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
            Console.Write(TetrisColors.AnsiReset);
            Console.CursorVisible = true;

            // Return to the original screen buffer
            if (_altScreenActive)
            {
                Console.Write("\x1b[?1049l");
                _altScreenActive = false;
            }
        }

        private void DrawBorder()
        {
            int boardCols = _boardWidth * 2 * _scale;
            int boardRows = _boardHeight * _scale;

            Console.Write(TetrisColors.AnsiBorder);

            Console.SetCursorPosition(0, 0);
            Console.Write("\u2554" + new string('\u2550', boardCols) + "\u2557");

            for (int r = 0; r < boardRows; r++)
            {
                Console.SetCursorPosition(0, r + 1);
                Console.Write("\u2551");
                Console.SetCursorPosition(boardCols + 1, r + 1);
                Console.Write("\u2551");
            }

            Console.SetCursorPosition(0, boardRows + 1);
            Console.Write("\u255a" + new string('\u2550', boardCols) + "\u255d");

            Console.Write(TetrisColors.AnsiReset);
        }
    }
}
