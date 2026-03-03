using System;

namespace PSTetris.Rendering
{
    public class SixelRenderer : IGameRenderer
    {
        private const int CellSize = 16;
        private const int BorderPx = 2;
        private const int BevelPx = 2;
        private const int GapPx = 8;          // gap between board and info panel
        private const int InfoPadding = 6;     // padding inside info panel
        private const int InfoInnerW = 100;    // inner width of info panel
        private const int FontScale = 2;       // 5x7 font at 2x = 10x14 pixels per glyph

        // Pixel regions (computed in Initialize)
        private int _boardPixelW;
        private int _boardPixelH;
        private int _infoPanelX;       // left edge of info panel in buffer
        private int _infoPanelW;       // total info panel width with border
        private int _totalPixelW;      // full buffer width
        private int _totalPixelH;      // full buffer height

        private int _boardWidth;
        private int _boardHeight;

        private PixelBuffer _buffer;
        private SixelEncoder _encoder;
        private int[,] _prevDisplay;

        // Cached info state for dirty detection
        private int _prevScore = -1;
        private int _prevLevel = -1;
        private int _prevLines = -1;
        private bool _prevPaused;
        private int _prevNextPieceType = -1;
        private bool _infoDirty;

        private int _bgReg;

        public void Initialize(int boardWidth, int boardHeight)
        {
            _boardWidth = boardWidth;
            _boardHeight = boardHeight;

            _boardPixelW = boardWidth * CellSize + 2 * BorderPx;
            _boardPixelH = boardHeight * CellSize + 2 * BorderPx;

            _infoPanelW = InfoInnerW + 2 * BorderPx;
            _infoPanelX = _boardPixelW + GapPx;
            _totalPixelW = _infoPanelX + _infoPanelW;
            _totalPixelH = _boardPixelH;

            _buffer = new PixelBuffer(_totalPixelW, _totalPixelH);
            _encoder = new SixelEncoder();
            _prevDisplay = new int[boardHeight, boardWidth];

            for (int r = 0; r < boardHeight; r++)
                for (int c = 0; c < boardWidth; c++)
                    _prevDisplay[r, c] = int.MinValue;

            SetupPalette();

            Console.CursorVisible = false;
            Console.Clear();

            // Fill entire buffer with background
            _buffer.FillRect(0, 0, _totalPixelW, _totalPixelH, TetrisColors.Background);

            // Draw board border and grid
            DrawBoardBorder();
            DrawGridLines();

            // Draw info panel border and static labels
            DrawInfoPanelFrame();

            _infoDirty = true;
            _buffer.MarkFullDirty();
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
            // Update board cells
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

        public void RenderGameOver(GameState state)
        {
            int boardInnerW = _boardWidth * CellSize;
            int boardInnerH = _boardHeight * CellSize;
            int overlayW = boardInnerW - 20;
            int overlayH = 50;
            int ox = BorderPx + 10;
            int oy = BorderPx + (boardInnerH - overlayH) / 2;

            // Dark overlay background
            _buffer.FillRect(ox, oy, overlayW, overlayH, TetrisColors.GameOverBg);

            // Border
            _buffer.FillRect(ox, oy, overlayW, 2, TetrisColors.GameOverText);
            _buffer.FillRect(ox, oy + overlayH - 2, overlayW, 2, TetrisColors.GameOverText);
            _buffer.FillRect(ox, oy, 2, overlayH, TetrisColors.GameOverText);
            _buffer.FillRect(ox + overlayW - 2, oy, 2, overlayH, TetrisColors.GameOverText);

            // "GAME OVER" text
            string msg1 = "GAME OVER";
            int tw1 = BitmapFont.MeasureWidth(msg1, FontScale);
            BitmapFont.DrawString(_buffer, ox + (overlayW - tw1) / 2, oy + 6,
                                  msg1, TetrisColors.GameOverText, FontScale);

            // Score
            string msg2 = "Score: " + state.Score;
            int tw2 = BitmapFont.MeasureWidth(msg2, FontScale);
            BitmapFont.DrawString(_buffer, ox + (overlayW - tw2) / 2, oy + 24,
                                  msg2, TetrisColors.GameOverText, FontScale);

            _buffer.MarkFullDirty();
            Console.SetCursorPosition(0, 0);
            Console.Write(_encoder.Encode(_buffer, 0, 0, _totalPixelW, _totalPixelH, _bgReg));
            _buffer.ResetDirty();

            while (Console.KeyAvailable) Console.ReadKey(intercept: true);
            Console.ReadKey(intercept: true);
        }

        public void Cleanup()
        {
            // Approximate terminal rows consumed by the Sixel image
            int terminalRows = _totalPixelH / 16 + 3;
            Console.SetCursorPosition(0, terminalRows);
            Console.Write(TetrisColors.AnsiReset);
            Console.CursorVisible = true;
        }

        // ——— Board pixel drawing ———

        private void DrawBoardBorder()
        {
            var bc = TetrisColors.BorderColor;
            _buffer.FillRect(0, 0, _boardPixelW, BorderPx, bc);
            _buffer.FillRect(0, _boardPixelH - BorderPx, _boardPixelW, BorderPx, bc);
            _buffer.FillRect(0, 0, BorderPx, _boardPixelH, bc);
            _buffer.FillRect(_boardPixelW - BorderPx, 0, BorderPx, _boardPixelH, bc);
        }

        private void DrawGridLines()
        {
            var gc = TetrisColors.GridLineColor;
            for (int c = 1; c < _boardWidth; c++)
            {
                int px = BorderPx + c * CellSize;
                _buffer.FillRect(px, BorderPx, 1, _boardHeight * CellSize, gc);
            }
            for (int r = 1; r < _boardHeight; r++)
            {
                int py = BorderPx + r * CellSize;
                _buffer.FillRect(BorderPx, py, _boardWidth * CellSize, 1, gc);
            }
        }

        private void DrawCell(int row, int col, int value)
        {
            int px = BorderPx + col * CellSize;
            int py = BorderPx + row * CellSize;

            if (value == 0)
            {
                _buffer.FillRect(px, py, CellSize, CellSize, TetrisColors.Background);
                if (col > 0)
                    _buffer.FillRect(px, py, 1, CellSize, TetrisColors.GridLineColor);
                if (row > 0)
                    _buffer.FillRect(px, py, CellSize, 1, TetrisColors.GridLineColor);
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

            _buffer.FillRect(px, py, CellSize, CellSize, baseColor);
            _buffer.FillRect(px, py, CellSize, BevelPx, light);
            _buffer.FillRect(px, py, BevelPx, CellSize, light);
            _buffer.FillRect(px, py + CellSize - BevelPx, CellSize, BevelPx, dark);
            _buffer.FillRect(px + CellSize - BevelPx, py, BevelPx, CellSize, dark);
        }

        private void DrawGhostCell(int px, int py)
        {
            _buffer.FillRect(px, py, CellSize, CellSize, TetrisColors.Background);
            var gc = TetrisColors.GhostColor;
            _buffer.FillRect(px, py, CellSize, 1, gc);
            _buffer.FillRect(px, py + CellSize - 1, CellSize, 1, gc);
            _buffer.FillRect(px, py, 1, CellSize, gc);
            _buffer.FillRect(px + CellSize - 1, py, 1, CellSize, gc);
        }

        // ——— Info panel pixel drawing ———

        // Layout constants (vertical positions within the info panel)
        // Each section: label (14px at 2x) + 2px gap + value (14px) + gap
        private const int Section0Y = 8;                    // SCORE label Y (relative to panel top)
        private const int SectionSpacing = 36;              // spacing between label groups
        private const int LabelValueGap = 16;               // gap between label and value line
        private const int NextSectionY = Section0Y + SectionSpacing * 3;  // NEXT section
        private const int NextPreviewY = NextSectionY + LabelValueGap;    // next piece preview area
        private const int NextPreviewSize = 4 * CellSize;                 // 4x4 cells for preview
        private const int ControlsSectionY = NextPreviewY + NextPreviewSize + 8;

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

            // Fill info panel background
            _buffer.FillRect(_infoPanelX, 0, _infoPanelW, _totalPixelH, bg);

            // Border
            _buffer.FillRect(_infoPanelX, 0, _infoPanelW, BorderPx, bc);
            _buffer.FillRect(_infoPanelX, _totalPixelH - BorderPx, _infoPanelW, BorderPx, bc);
            _buffer.FillRect(_infoPanelX, 0, BorderPx, _totalPixelH, bc);
            _buffer.FillRect(_infoPanelX + _infoPanelW - BorderPx, 0, BorderPx, _totalPixelH, bc);

            int innerX = _infoPanelX + BorderPx + InfoPadding;

            // Draw static labels
            for (int i = 0; i < _labels.Length; i++)
            {
                int ly = Section0Y + i * SectionSpacing;
                BitmapFont.DrawString(_buffer, innerX, ly, _labels[i],
                                      TetrisColors.InfoLabel, FontScale);
            }

            // "NEXT" label
            BitmapFont.DrawString(_buffer, innerX, NextSectionY, "NEXT",
                                  TetrisColors.InfoLabel, FontScale);

            // Separator line between stats and next piece
            int sepY = NextSectionY - 4;
            _buffer.FillRect(_infoPanelX + BorderPx + 2, sepY,
                             _infoPanelW - 2 * BorderPx - 4, 1, TetrisColors.InfoPanelBorder);

            // Controls section
            // Separator above controls
            int ctrlSepY = ControlsSectionY - 4;
            _buffer.FillRect(_infoPanelX + BorderPx + 2, ctrlSepY,
                             _infoPanelW - 2 * BorderPx - 4, 1, TetrisColors.InfoPanelBorder);

            for (int i = 0; i < _controls.Length; i++)
            {
                int cy = ControlsSectionY + i * (BitmapFont.GlyphHeight + 3);
                BitmapFont.DrawString(_buffer, innerX, cy, _controls[i],
                                      TetrisColors.InfoLabel, 1);  // scale 1 for controls (smaller)
            }
        }

        private void DrawInfoValue(int index, string value)
        {
            int innerX = _infoPanelX + BorderPx + InfoPadding;
            int vy = Section0Y + index * SectionSpacing + LabelValueGap;

            // Clear value area
            int clearW = InfoInnerW - InfoPadding * 2;
            int clearH = BitmapFont.GlyphHeight * FontScale;
            _buffer.FillRect(innerX, vy, clearW, clearH, TetrisColors.InfoPanelBg);

            // Draw new value
            BitmapFont.DrawString(_buffer, innerX, vy, value,
                                  TetrisColors.InfoValue, FontScale);
        }

        private void DrawNextPiecePreview(Tetromino piece)
        {
            int innerX = _infoPanelX + BorderPx + InfoPadding;

            // Clear preview area
            _buffer.FillRect(innerX, NextPreviewY,
                             NextPreviewSize, NextPreviewSize, TetrisColors.InfoPanelBg);

            if (piece == null) return;

            // Center the piece in the 4x4 preview area
            var cells = piece.GetCells();
            var baseColor = TetrisColors.PieceColors[(int)piece.Type];
            var light = baseColor.Lighten(0.4f);
            var dark = baseColor.Darken(0.4f);

            foreach (var cell in cells)
            {
                int r = cell[0], c = cell[1];
                if (r < 0 || r >= 4 || c < 0 || c >= 4) continue;

                int px = innerX + c * CellSize;
                int py = NextPreviewY + r * CellSize;

                // Draw with bevel, same as board pieces
                _buffer.FillRect(px, py, CellSize, CellSize, baseColor);
                _buffer.FillRect(px, py, CellSize, BevelPx, light);
                _buffer.FillRect(px, py, BevelPx, CellSize, light);
                _buffer.FillRect(px, py + CellSize - BevelPx, CellSize, BevelPx, dark);
                _buffer.FillRect(px + CellSize - BevelPx, py, BevelPx, CellSize, dark);
            }
        }

        private void DrawPauseOverlay(bool paused)
        {
            int boardInnerW = _boardWidth * CellSize;
            int boardInnerH = _boardHeight * CellSize;

            if (paused)
            {
                string msg = "PAUSED";
                int tw = BitmapFont.MeasureWidth(msg, FontScale);
                int overlayW = tw + 20;
                int overlayH = BitmapFont.GlyphHeight * FontScale + 12;
                int ox = BorderPx + (boardInnerW - overlayW) / 2;
                int oy = BorderPx + (boardInnerH - overlayH) / 2;

                _buffer.FillRect(ox, oy, overlayW, overlayH, TetrisColors.PausedBg);
                // Border
                _buffer.FillRect(ox, oy, overlayW, 2, TetrisColors.PausedText);
                _buffer.FillRect(ox, oy + overlayH - 2, overlayW, 2, TetrisColors.PausedText);
                _buffer.FillRect(ox, oy, 2, overlayH, TetrisColors.PausedText);
                _buffer.FillRect(ox + overlayW - 2, oy, 2, overlayH, TetrisColors.PausedText);

                BitmapFont.DrawString(_buffer, ox + 10, oy + 6, msg,
                                      TetrisColors.PausedText, FontScale);
            }
            else
            {
                // Unpause: redraw all board cells to clear overlay
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
