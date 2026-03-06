using System;

namespace PSTetris.Rendering
{
    public class SixelRenderer : IGameRenderer
    {
        // Computed in Initialize based on terminal size
        private int _cellSize;
        private int _borderPx;
        private int _bevelPx;
        private int _gapPx;
        private int _infoPadding;
        private int _infoInnerW;
        private int _fontScale;

        // Layout positions (computed from _cellSize)
        private int _section0Y;
        private int _sectionSpacing;
        private int _labelValueGap;
        private int _nextSectionY;
        private int _nextPreviewY;
        private int _nextPreviewSize;
        private int _controlsSectionY;
        private int _controlsFontScale;
        private int _controlsLineH;

        // Pixel regions
        private int _boardPixelW;
        private int _boardPixelH;
        private int _infoPanelX;
        private int _infoPanelW;
        private int _totalPixelW;
        private int _totalPixelH;

        private int _boardWidth;
        private int _boardHeight;

        private PixelBuffer _buffer;
        private SixelEncoder _encoder;
        private int[,] _prevDisplay;
        private bool _altScreenActive;

        // Cached info state for dirty detection
        private int _prevScore = -1;
        private int _prevLevel = -1;
        private int _prevLines = -1;
        private bool _prevPaused;
        private int _prevNextPieceType = -1;
        private bool _infoDirty;

        private int _bgReg;

        // Terminal cell pixel height for cleanup cursor positioning
        private int _termCellPixelH = 16;

        public void Initialize(int boardWidth, int boardHeight)
        {
            _boardWidth = boardWidth;
            _boardHeight = boardHeight;

            // Detect terminal pixel dimensions
            TerminalCapabilities.DetectTerminalSize(
                out int cols, out int rows, out int pixelW, out int pixelH);

            if (rows > 0)
                _termCellPixelH = Math.Max(8, pixelH / rows);

            // Compute optimal cell size
            // totalW = cell * (boardW + borderFrac + gapFrac + infoFrac + borderFrac)
            //        ≈ cell * (boardW + 7.25)
            // totalH = cell * (boardH + borderFrac)
            //        ≈ cell * (boardH + 0.25)
            double ratioW = boardWidth + 7.25;
            double ratioH = boardHeight + 0.25;
            int maxFromW = (int)(pixelW / ratioW);
            int maxFromH = (int)(pixelH / ratioH);
            _cellSize = Math.Min(maxFromW, maxFromH);
            _cellSize = Math.Max(8, Math.Min(32, _cellSize));

            ComputeLayout();

            _boardPixelW = boardWidth * _cellSize + 2 * _borderPx;
            _boardPixelH = boardHeight * _cellSize + 2 * _borderPx;

            _infoPanelW = _infoInnerW + 2 * _borderPx;
            _infoPanelX = _boardPixelW + _gapPx;
            _totalPixelW = _infoPanelX + _infoPanelW;
            _totalPixelH = _boardPixelH;

            _buffer = new PixelBuffer(_totalPixelW, _totalPixelH);
            _encoder = new SixelEncoder();
            _prevDisplay = new int[boardHeight, boardWidth];

            for (int r = 0; r < boardHeight; r++)
                for (int c = 0; c < boardWidth; c++)
                    _prevDisplay[r, c] = int.MinValue;

            _prevScore = -1;
            _prevLevel = -1;
            _prevLines = -1;
            _prevPaused = false;
            _prevNextPieceType = -1;

            SetupPalette();

            // Switch to alternate screen buffer on first init
            if (!_altScreenActive)
            {
                Console.Write("\x1b[?1049h");
                _altScreenActive = true;
            }

            Console.CursorVisible = false;
            Console.Clear();

            _buffer.FillRect(0, 0, _totalPixelW, _totalPixelH, TetrisColors.Background);
            DrawBoardBorder();
            DrawGridLines();
            DrawInfoPanelFrame();

            _infoDirty = true;
            _buffer.MarkFullDirty();
        }

        private void ComputeLayout()
        {
            _borderPx       = Math.Max(1, _cellSize / 8);
            _bevelPx        = Math.Max(1, _cellSize / 8);
            _gapPx          = Math.Max(2, _cellSize / 2);
            _infoPadding    = Math.Max(2, _cellSize * 3 / 8);
            _fontScale      = Math.Max(1, _cellSize / 8);
            _infoInnerW     = Math.Max(50, _cellSize * 25 / 4);

            _section0Y          = _cellSize / 2;
            _sectionSpacing     = _cellSize * 9 / 4;
            _labelValueGap      = _cellSize;
            _nextSectionY       = _section0Y + _sectionSpacing * 3;
            _nextPreviewY       = _nextSectionY + _labelValueGap;
            _nextPreviewSize    = 4 * _cellSize;
            _controlsSectionY   = _nextPreviewY + _nextPreviewSize + _cellSize / 2;

            _controlsFontScale  = Math.Max(1, _fontScale - 1);
            _controlsLineH      = BitmapFont.GlyphHeight * _controlsFontScale
                                   + Math.Max(1, _cellSize / 8);
        }

        private void SetupPalette()
        {
            _bgReg = _encoder.RegisterColor(TetrisColors.Background);
            _encoder.RegisterColor(TetrisColors.BorderColor);
            _encoder.RegisterColor(TetrisColors.GhostColor);
            _encoder.RegisterColor(TetrisColors.GridLineColor);
            _encoder.RegisterColor(TetrisColors.InfoPanelBg);
            _encoder.RegisterColor(TetrisColors.InfoPanelBorder);
            _encoder.RegisterColor(TetrisColors.InfoLabel);
            _encoder.RegisterColor(TetrisColors.InfoValue);
            _encoder.RegisterColor(TetrisColors.PausedBg);
            _encoder.RegisterColor(TetrisColors.PausedText);
            _encoder.RegisterColor(TetrisColors.GameOverBg);
            _encoder.RegisterColor(TetrisColors.GameOverText);

            for (int i = 0; i < 8; i++)
            {
                _encoder.RegisterColor(TetrisColors.PieceColors[i]);
                if (i > 0)
                {
                    _encoder.RegisterColor(TetrisColors.PieceColors[i].Lighten(0.4f));
                    _encoder.RegisterColor(TetrisColors.PieceColors[i].Darken(0.4f));
                }
            }
        }

        public void RenderFrame(GameState state)
        {
            bool boardChanged = false;
            for (int r = 0; r < _boardHeight; r++)
            {
                for (int c = 0; c < _boardWidth; c++)
                {
                    int cur = state.Display[r, c];
                    if (cur != _prevDisplay[r, c])
                    {
                        _prevDisplay[r, c] = cur;
                        DrawCell(r, c, cur);
                        boardChanged = true;
                    }
                }
            }

            if (!boardChanged && !_infoDirty && !_buffer.HasDirty)
                return;

            _infoDirty = false;
            Console.SetCursorPosition(0, 0);

            string sixel = _encoder.Encode(_buffer,
                0, 0, _totalPixelW, _totalPixelH, _bgReg);

            Console.Write(sixel);
            _buffer.ResetDirty();
        }

        public void RenderInfo(GameState state)
        {
            bool changed = false;

            if (state.Score != _prevScore)
            {
                _prevScore = state.Score;
                DrawInfoValue(0, state.Score.ToString());
                changed = true;
            }

            if (state.Level != _prevLevel)
            {
                _prevLevel = state.Level;
                DrawInfoValue(1, state.Level.ToString());
                changed = true;
            }

            if (state.Lines != _prevLines)
            {
                _prevLines = state.Lines;
                DrawInfoValue(2, state.Lines.ToString());
                changed = true;
            }

            int nextType = state.NextPiece != null ? (int)state.NextPiece.Type : 0;
            if (nextType != _prevNextPieceType)
            {
                _prevNextPieceType = nextType;
                DrawNextPiecePreview(state.NextPiece);
                changed = true;
            }

            if (state.Paused != _prevPaused)
            {
                _prevPaused = state.Paused;
                DrawPauseOverlay(state.Paused);
                changed = true;
            }

            if (changed)
                _infoDirty = true;
        }

        public bool RenderGameOver(GameState state)
        {
            int boardInnerW = _boardWidth * _cellSize;
            int boardInnerH = _boardHeight * _cellSize;
            int overlayW = boardInnerW - _cellSize * 5 / 4;
            int lineH = BitmapFont.GlyphHeight * _fontScale + _cellSize / 4;
            int overlayH = lineH * 4 + _cellSize / 2;
            int ox = _borderPx + _cellSize * 5 / 8;
            int oy = _borderPx + (boardInnerH - overlayH) / 2;

            // Dark overlay background
            _buffer.FillRect(ox, oy, overlayW, overlayH, TetrisColors.GameOverBg);

            // Border
            _buffer.FillRect(ox, oy, overlayW, _borderPx, TetrisColors.GameOverText);
            _buffer.FillRect(ox, oy + overlayH - _borderPx, overlayW, _borderPx, TetrisColors.GameOverText);
            _buffer.FillRect(ox, oy, _borderPx, overlayH, TetrisColors.GameOverText);
            _buffer.FillRect(ox + overlayW - _borderPx, oy, _borderPx, overlayH, TetrisColors.GameOverText);

            int textY = oy + _cellSize * 3 / 8;

            // "GAME OVER"
            string msg1 = "GAME OVER";
            int tw1 = BitmapFont.MeasureWidth(msg1, _fontScale);
            BitmapFont.DrawString(_buffer, ox + (overlayW - tw1) / 2, textY,
                                  msg1, TetrisColors.GameOverText, _fontScale);

            // Score
            textY += lineH;
            string msg2 = "Score: " + state.Score;
            int tw2 = BitmapFont.MeasureWidth(msg2, _fontScale);
            BitmapFont.DrawString(_buffer, ox + (overlayW - tw2) / 2, textY,
                                  msg2, TetrisColors.GameOverText, _fontScale);

            // Restart
            textY += lineH;
            string msg3 = "R: Restart";
            int tw3 = BitmapFont.MeasureWidth(msg3, _fontScale);
            BitmapFont.DrawString(_buffer, ox + (overlayW - tw3) / 2, textY,
                                  msg3, TetrisColors.GameOverText, _fontScale);

            // Quit
            textY += lineH;
            string msg4 = "Q: Quit   ";
            int tw4 = BitmapFont.MeasureWidth(msg4, _fontScale);
            BitmapFont.DrawString(_buffer, ox + (overlayW - tw4) / 2, textY,
                                  msg4, TetrisColors.GameOverText, _fontScale);

            _buffer.MarkFullDirty();
            Console.SetCursorPosition(0, 0);
            Console.Write(_encoder.Encode(_buffer, 0, 0, _totalPixelW, _totalPixelH, _bgReg));
            _buffer.ResetDirty();

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

        // ——— Board pixel drawing ———

        private void DrawBoardBorder()
        {
            var bc = TetrisColors.BorderColor;
            _buffer.FillRect(0, 0, _boardPixelW, _borderPx, bc);
            _buffer.FillRect(0, _boardPixelH - _borderPx, _boardPixelW, _borderPx, bc);
            _buffer.FillRect(0, 0, _borderPx, _boardPixelH, bc);
            _buffer.FillRect(_boardPixelW - _borderPx, 0, _borderPx, _boardPixelH, bc);
        }

        private void DrawGridLines()
        {
            var gc = TetrisColors.GridLineColor;
            for (int c = 1; c < _boardWidth; c++)
            {
                int px = _borderPx + c * _cellSize;
                _buffer.FillRect(px, _borderPx, 1, _boardHeight * _cellSize, gc);
            }
            for (int r = 1; r < _boardHeight; r++)
            {
                int py = _borderPx + r * _cellSize;
                _buffer.FillRect(_borderPx, py, _boardWidth * _cellSize, 1, gc);
            }
        }

        private void DrawCell(int row, int col, int value)
        {
            int px = _borderPx + col * _cellSize;
            int py = _borderPx + row * _cellSize;

            if (value == 0)
            {
                _buffer.FillRect(px, py, _cellSize, _cellSize, TetrisColors.Background);
                if (col > 0)
                    _buffer.FillRect(px, py, 1, _cellSize, TetrisColors.GridLineColor);
                if (row > 0)
                    _buffer.FillRect(px, py, _cellSize, 1, TetrisColors.GridLineColor);
            }
            else if (value == -1)
            {
                DrawGhostCell(px, py);
            }
            else
            {
                DrawFilledCell(px, py, value);
            }
        }

        private void DrawFilledCell(int px, int py, int pieceType)
        {
            var baseColor = TetrisColors.PieceColors[pieceType];
            var light = baseColor.Lighten(0.4f);
            var dark = baseColor.Darken(0.4f);

            _buffer.FillRect(px, py, _cellSize, _cellSize, baseColor);
            _buffer.FillRect(px, py, _cellSize, _bevelPx, light);
            _buffer.FillRect(px, py, _bevelPx, _cellSize, light);
            _buffer.FillRect(px, py + _cellSize - _bevelPx, _cellSize, _bevelPx, dark);
            _buffer.FillRect(px + _cellSize - _bevelPx, py, _bevelPx, _cellSize, dark);
        }

        private void DrawGhostCell(int px, int py)
        {
            _buffer.FillRect(px, py, _cellSize, _cellSize, TetrisColors.Background);
            var gc = TetrisColors.GhostColor;
            _buffer.FillRect(px, py, _cellSize, 1, gc);
            _buffer.FillRect(px, py + _cellSize - 1, _cellSize, 1, gc);
            _buffer.FillRect(px, py, 1, _cellSize, gc);
            _buffer.FillRect(px + _cellSize - 1, py, 1, _cellSize, gc);
        }

        // ——— Info panel pixel drawing ———

        private static readonly string[] _labels = { "SCORE", "LEVEL", "LINES" };
        private static readonly string[] _controls =
        {
            "Move    AD",
            "Rotate  W",
            "Drop    Spc",
            "Soft    S",
            "Pause   P",
            "Quit    Esc",
        };

        private void DrawInfoPanelFrame()
        {
            var bg = TetrisColors.InfoPanelBg;
            var bc = TetrisColors.InfoPanelBorder;

            _buffer.FillRect(_infoPanelX, 0, _infoPanelW, _totalPixelH, bg);

            // Border
            _buffer.FillRect(_infoPanelX, 0, _infoPanelW, _borderPx, bc);
            _buffer.FillRect(_infoPanelX, _totalPixelH - _borderPx, _infoPanelW, _borderPx, bc);
            _buffer.FillRect(_infoPanelX, 0, _borderPx, _totalPixelH, bc);
            _buffer.FillRect(_infoPanelX + _infoPanelW - _borderPx, 0, _borderPx, _totalPixelH, bc);

            int innerX = _infoPanelX + _borderPx + _infoPadding;

            // Draw static labels
            for (int i = 0; i < _labels.Length; i++)
            {
                int ly = _section0Y + i * _sectionSpacing;
                BitmapFont.DrawString(_buffer, innerX, ly, _labels[i],
                                      TetrisColors.InfoLabel, _fontScale);
            }

            // "NEXT" label
            BitmapFont.DrawString(_buffer, innerX, _nextSectionY, "NEXT",
                                  TetrisColors.InfoLabel, _fontScale);

            // Separator line between stats and next piece
            int sepY = _nextSectionY - Math.Max(1, _cellSize / 4);
            _buffer.FillRect(_infoPanelX + _borderPx + 2, sepY,
                             _infoPanelW - 2 * _borderPx - 4, 1, TetrisColors.InfoPanelBorder);

            // Controls section (only if it fits)
            int controlsEndY = _controlsSectionY + _controls.Length * _controlsLineH;
            if (controlsEndY < _totalPixelH - _borderPx)
            {
                int ctrlSepY = _controlsSectionY - Math.Max(1, _cellSize / 4);
                _buffer.FillRect(_infoPanelX + _borderPx + 2, ctrlSepY,
                                 _infoPanelW - 2 * _borderPx - 4, 1, TetrisColors.InfoPanelBorder);

                for (int i = 0; i < _controls.Length; i++)
                {
                    int cy = _controlsSectionY + i * _controlsLineH;
                    BitmapFont.DrawString(_buffer, innerX, cy, _controls[i],
                                          TetrisColors.InfoLabel, _controlsFontScale);
                }
            }
        }

        private void DrawInfoValue(int index, string value)
        {
            int innerX = _infoPanelX + _borderPx + _infoPadding;
            int vy = _section0Y + index * _sectionSpacing + _labelValueGap;

            int clearW = _infoInnerW - _infoPadding * 2;
            int clearH = BitmapFont.GlyphHeight * _fontScale;
            _buffer.FillRect(innerX, vy, clearW, clearH, TetrisColors.InfoPanelBg);

            BitmapFont.DrawString(_buffer, innerX, vy, value,
                                  TetrisColors.InfoValue, _fontScale);
        }

        private void DrawNextPiecePreview(Tetromino piece)
        {
            int innerX = _infoPanelX + _borderPx + _infoPadding;

            _buffer.FillRect(innerX, _nextPreviewY,
                             _nextPreviewSize, _nextPreviewSize, TetrisColors.InfoPanelBg);

            if (piece == null) return;

            var cells = piece.GetCells();
            var baseColor = TetrisColors.PieceColors[(int)piece.Type];
            var light = baseColor.Lighten(0.4f);
            var dark = baseColor.Darken(0.4f);

            foreach (var cell in cells)
            {
                int r = cell[0], c = cell[1];
                if (r < 0 || r >= 4 || c < 0 || c >= 4) continue;

                int px = innerX + c * _cellSize;
                int py = _nextPreviewY + r * _cellSize;

                _buffer.FillRect(px, py, _cellSize, _cellSize, baseColor);
                _buffer.FillRect(px, py, _cellSize, _bevelPx, light);
                _buffer.FillRect(px, py, _bevelPx, _cellSize, light);
                _buffer.FillRect(px, py + _cellSize - _bevelPx, _cellSize, _bevelPx, dark);
                _buffer.FillRect(px + _cellSize - _bevelPx, py, _bevelPx, _cellSize, dark);
            }
        }

        private void DrawPauseOverlay(bool paused)
        {
            int boardInnerW = _boardWidth * _cellSize;
            int boardInnerH = _boardHeight * _cellSize;

            if (paused)
            {
                string msg = "PAUSED";
                int tw = BitmapFont.MeasureWidth(msg, _fontScale);
                int overlayW = tw + _cellSize * 5 / 4;
                int overlayH = BitmapFont.GlyphHeight * _fontScale + _cellSize * 3 / 4;
                int ox = _borderPx + (boardInnerW - overlayW) / 2;
                int oy = _borderPx + (boardInnerH - overlayH) / 2;

                _buffer.FillRect(ox, oy, overlayW, overlayH, TetrisColors.PausedBg);
                _buffer.FillRect(ox, oy, overlayW, _borderPx, TetrisColors.PausedText);
                _buffer.FillRect(ox, oy + overlayH - _borderPx, overlayW, _borderPx, TetrisColors.PausedText);
                _buffer.FillRect(ox, oy, _borderPx, overlayH, TetrisColors.PausedText);
                _buffer.FillRect(ox + overlayW - _borderPx, oy, _borderPx, overlayH, TetrisColors.PausedText);

                BitmapFont.DrawString(_buffer, ox + _cellSize * 5 / 8, oy + _cellSize * 3 / 8, msg,
                                      TetrisColors.PausedText, _fontScale);
            }
            else
            {
                for (int r = 0; r < _boardHeight; r++)
                    for (int c = 0; c < _boardWidth; c++)
                    {
                        DrawCell(r, c, _prevDisplay[r, c]);
                    }
                DrawGridLines();
            }
        }
    }
}
