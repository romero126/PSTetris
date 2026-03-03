namespace PSTetris.Rendering
{
    public class GameState
    {
        public int BoardWidth { get; }
        public int BoardHeight { get; }

        /// <summary>
        /// Composite display grid. Values: 0=empty, 1-7=piece type, -1=ghost.
        /// Dimensions: [Height, Width]
        /// </summary>
        public int[,] Display { get; }

        public int Score { get; }
        public int Level { get; }
        public int Lines { get; }
        public bool Paused { get; }
        public bool GameOver { get; }
        public Tetromino NextPiece { get; }

        public GameState(int boardWidth, int boardHeight, int[,] display,
                         int score, int level, int lines,
                         bool paused, bool gameOver, Tetromino nextPiece)
        {
            BoardWidth = boardWidth;
            BoardHeight = boardHeight;
            Display = display;
            Score = score;
            Level = level;
            Lines = lines;
            Paused = paused;
            GameOver = gameOver;
            NextPiece = nextPiece;
        }
    }
}
