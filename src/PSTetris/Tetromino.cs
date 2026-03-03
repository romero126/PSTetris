using System;

namespace PSTetris
{
    public enum TetrominoType { I = 1, O = 2, T = 3, S = 4, Z = 5, J = 6, L = 7 }

    public class Tetromino
    {
        // Shape data: [typeIndex][rotation][cellIndex][0=row / 1=col]
        // I uses a 4x4 bounding box; all others use 3x3.
        private static readonly int[][][][] _shapes =
        {
            // I
            new[]
            {
                new[]{ new[]{1,0}, new[]{1,1}, new[]{1,2}, new[]{1,3} },
                new[]{ new[]{0,2}, new[]{1,2}, new[]{2,2}, new[]{3,2} },
                new[]{ new[]{2,0}, new[]{2,1}, new[]{2,2}, new[]{2,3} },
                new[]{ new[]{0,1}, new[]{1,1}, new[]{2,1}, new[]{3,1} },
            },
            // O
            new[]
            {
                new[]{ new[]{0,0}, new[]{0,1}, new[]{1,0}, new[]{1,1} },
                new[]{ new[]{0,0}, new[]{0,1}, new[]{1,0}, new[]{1,1} },
                new[]{ new[]{0,0}, new[]{0,1}, new[]{1,0}, new[]{1,1} },
                new[]{ new[]{0,0}, new[]{0,1}, new[]{1,0}, new[]{1,1} },
            },
            // T
            new[]
            {
                new[]{ new[]{0,1}, new[]{1,0}, new[]{1,1}, new[]{1,2} },
                new[]{ new[]{0,1}, new[]{1,1}, new[]{1,2}, new[]{2,1} },
                new[]{ new[]{1,0}, new[]{1,1}, new[]{1,2}, new[]{2,1} },
                new[]{ new[]{0,1}, new[]{1,0}, new[]{1,1}, new[]{2,1} },
            },
            // S
            new[]
            {
                new[]{ new[]{0,1}, new[]{0,2}, new[]{1,0}, new[]{1,1} },
                new[]{ new[]{0,1}, new[]{1,1}, new[]{1,2}, new[]{2,2} },
                new[]{ new[]{1,1}, new[]{1,2}, new[]{2,0}, new[]{2,1} },
                new[]{ new[]{0,0}, new[]{1,0}, new[]{1,1}, new[]{2,1} },
            },
            // Z
            new[]
            {
                new[]{ new[]{0,0}, new[]{0,1}, new[]{1,1}, new[]{1,2} },
                new[]{ new[]{0,2}, new[]{1,1}, new[]{1,2}, new[]{2,1} },
                new[]{ new[]{1,0}, new[]{1,1}, new[]{2,1}, new[]{2,2} },
                new[]{ new[]{0,1}, new[]{1,0}, new[]{1,1}, new[]{2,0} },
            },
            // J
            new[]
            {
                new[]{ new[]{0,0}, new[]{1,0}, new[]{1,1}, new[]{1,2} },
                new[]{ new[]{0,1}, new[]{0,2}, new[]{1,1}, new[]{2,1} },
                new[]{ new[]{1,0}, new[]{1,1}, new[]{1,2}, new[]{2,2} },
                new[]{ new[]{0,1}, new[]{1,1}, new[]{2,0}, new[]{2,1} },
            },
            // L
            new[]
            {
                new[]{ new[]{0,2}, new[]{1,0}, new[]{1,1}, new[]{1,2} },
                new[]{ new[]{0,1}, new[]{1,1}, new[]{2,1}, new[]{2,2} },
                new[]{ new[]{1,0}, new[]{1,1}, new[]{1,2}, new[]{2,0} },
                new[]{ new[]{0,0}, new[]{0,1}, new[]{1,1}, new[]{2,1} },
            },
        };

        // Spawn column (left of bounding box) per type
        private static readonly int[] _spawnCols = { 3, 4, 3, 3, 3, 3, 3 };
        // Spawn row per type (-1 = piece starts partially above the visible board)
        private static readonly int[] _spawnRows = { -1, 0, 0, 0, 0, 0, 0 };

        public TetrominoType Type { get; private set; }
        public int Rotation { get; set; }
        public int Row { get; set; }
        public int Col { get; set; }

        public Tetromino(TetrominoType type)
        {
            Type = type;
            int idx = (int)type - 1;
            Row = _spawnRows[idx];
            Col = _spawnCols[idx];
        }

        public int[][] GetCells() => _shapes[(int)Type - 1][Rotation];

        public (int row, int col)[] GetAbsoluteCells()
        {
            var cells = GetCells();
            return new[]
            {
                (Row + cells[0][0], Col + cells[0][1]),
                (Row + cells[1][0], Col + cells[1][1]),
                (Row + cells[2][0], Col + cells[2][1]),
                (Row + cells[3][0], Col + cells[3][1]),
            };
        }

        public Tetromino Clone()
        {
            var t = new Tetromino(Type) { Row = Row, Col = Col, Rotation = Rotation };
            return t;
        }

        public static TetrominoType RandomType(Random rng) => (TetrominoType)(rng.Next(7) + 1);
    }
}
