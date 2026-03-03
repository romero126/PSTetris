using System;
using System.Threading;
using PSTetris.Rendering;

namespace PSTetris
{
    public class TetrisGame
    {
        // Board dimensions
        private const int Width = 10;
        private const int Height = 20;

        // ——— State ———
        private readonly int[,] _board = new int[Height, Width];
        private Tetromino _current;
        private Tetromino _next;
        private readonly Random _rng = new Random();

        private int _score;
        private int _level = 1;
        private int _lines;
        private bool _gameOver;
        private bool _paused;

        private readonly IGameRenderer _renderer;

        // Fall speed in ms; decreases every level
        private int FallInterval => Math.Max(80, 800 - (_level - 1) * 70);

        public TetrisGame(IGameRenderer renderer = null)
        {
            _renderer = renderer ?? new AnsiRenderer();
        }

        // ——————————————————————————————
        public void Run()
        {
            if (!Environment.UserInteractive)
            {
                Console.WriteLine("PSTetris requires an interactive console.");
                return;
            }

            _renderer.Initialize(Width, Height);

            try
            {
                _next = new Tetromino(Tetromino.RandomType(_rng));
                SpawnPiece();
                if (_gameOver) return;

                _renderer.RenderInfo(BuildGameState());
                _renderer.RenderFrame(BuildGameState());

                var nextFall = DateTime.UtcNow.AddMilliseconds(FallInterval);

                while (!_gameOver)
                {
                    // --- Input ---
                    while (Console.KeyAvailable)
                    {
                        var k = Console.ReadKey(intercept: true);
                        HandleInput(k.Key, ref nextFall);
                        if (_gameOver) break;
                    }

                    if (_gameOver) break;

                    // --- Gravity ---
                    if (!_paused && DateTime.UtcNow >= nextFall)
                    {
                        if (!TryMove(1, 0))
                            LockPiece();
                        else
                            nextFall = DateTime.UtcNow.AddMilliseconds(FallInterval);
                    }

                    _renderer.RenderFrame(BuildGameState());
                    Thread.Sleep(16);   // ~60 fps cap
                }

                _renderer.RenderFrame(BuildGameState());
                _renderer.RenderGameOver(BuildGameState());
            }
            finally
            {
                _renderer.Cleanup();
            }
        }

        // ——— Game state snapshot for renderer ———
        private GameState BuildGameState()
        {
            var display = new int[Height, Width];
            for (int r = 0; r < Height; r++)
                for (int c = 0; c < Width; c++)
                    display[r, c] = _board[r, c];

            if (_current != null)
            {
                // Ghost piece
                var ghost = _current.Clone();
                while (true)
                {
                    var test = ghost.Clone();
                    test.Row++;
                    if (!IsValid(test)) break;
                    ghost = test;
                }
                if (ghost.Row != _current.Row)
                {
                    foreach (var (r, c) in ghost.GetAbsoluteCells())
                        if (r >= 0 && r < Height && c >= 0 && c < Width && display[r, c] == 0)
                            display[r, c] = -1;
                }

                // Active piece on top
                foreach (var (r, c) in _current.GetAbsoluteCells())
                    if (r >= 0 && r < Height && c >= 0 && c < Width)
                        display[r, c] = (int)_current.Type;
            }

            return new GameState(Width, Height, display,
                                 _score, _level, _lines,
                                 _paused, _gameOver, _next);
        }

        // ——— Input ——————————————————
        private void HandleInput(ConsoleKey key, ref DateTime nextFall)
        {
            if (_paused && key != ConsoleKey.P && key != ConsoleKey.Escape)
                return;

            switch (key)
            {
                case ConsoleKey.LeftArrow:
                case ConsoleKey.A:
                    TryMove(0, -1);
                    break;

                case ConsoleKey.RightArrow:
                case ConsoleKey.D:
                    TryMove(0, 1);
                    break;

                case ConsoleKey.UpArrow:
                case ConsoleKey.W:
                    TryRotate(1);
                    break;

                case ConsoleKey.Z:
                case ConsoleKey.X:
                    TryRotate(-1);
                    break;

                case ConsoleKey.DownArrow:
                case ConsoleKey.S:
                    if (TryMove(1, 0))
                    {
                        _score += 1;
                        nextFall = DateTime.UtcNow.AddMilliseconds(FallInterval);
                        _renderer.RenderInfo(BuildGameState());
                    }
                    else
                    {
                        LockPiece();
                    }
                    break;

                case ConsoleKey.Spacebar:
                    HardDrop();
                    break;

                case ConsoleKey.P:
                    _paused = !_paused;
                    _renderer.RenderInfo(BuildGameState());
                    break;

                case ConsoleKey.Escape:
                case ConsoleKey.Q:
                    _gameOver = true;
                    break;
            }
        }

        // ——— Movement ——————————————
        private bool TryMove(int dRow, int dCol)
        {
            var clone = _current.Clone();
            clone.Row += dRow;
            clone.Col += dCol;
            if (!IsValid(clone)) return false;
            _current = clone;
            return true;
        }

        private bool TryRotate(int dir)
        {
            var clone = _current.Clone();
            clone.Rotation = (clone.Rotation + dir + 4) % 4;

            if (IsValid(clone)) { _current = clone; return true; }

            foreach (int kick in new[] { 1, -1, 2, -2 })
            {
                var kicked = clone.Clone();
                kicked.Col += kick;
                if (IsValid(kicked)) { _current = kicked; return true; }
            }

            return false;
        }

        private void HardDrop()
        {
            int dropped = 0;
            while (TryMove(1, 0)) dropped++;
            _score += dropped * 2;
            LockPiece();
        }

        // ——— Lock & Spawn —————————
        private void LockPiece()
        {
            foreach (var (r, c) in _current.GetAbsoluteCells())
            {
                if (r < 0) { _gameOver = true; return; }
                _board[r, c] = (int)_current.Type;
            }

            int cleared = ClearLines();
            AddScore(cleared);
            SpawnPiece();
        }

        private void SpawnPiece()
        {
            _current = _next;
            _next = new Tetromino(Tetromino.RandomType(_rng));
            if (!IsValid(_current))
                _gameOver = true;
            _renderer.RenderInfo(BuildGameState());
        }

        private int ClearLines()
        {
            int cleared = 0;
            for (int r = Height - 1; r >= 0; r--)
            {
                bool full = true;
                for (int c = 0; c < Width; c++)
                    if (_board[r, c] == 0) { full = false; break; }

                if (!full) continue;
                for (int rr = r; rr > 0; rr--)
                    for (int c = 0; c < Width; c++)
                        _board[rr, c] = _board[rr - 1, c];
                for (int c = 0; c < Width; c++) _board[0, c] = 0;
                cleared++;
                r++;
            }
            return cleared;
        }

        private void AddScore(int cleared)
        {
            int[] pts = { 0, 100, 300, 500, 800 };
            _score += pts[Math.Min(cleared, 4)] * _level;
            _lines  += cleared;
            _level   = _lines / 10 + 1;
        }

        // ——— Validation ————————————
        private bool IsValid(Tetromino piece)
        {
            foreach (var (r, c) in piece.GetAbsoluteCells())
            {
                if (c < 0 || c >= Width)  return false;
                if (r >= Height)          return false;
                if (r >= 0 && _board[r, c] != 0) return false;
            }
            return true;
        }
    }
}
