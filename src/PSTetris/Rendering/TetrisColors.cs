using System;

namespace PSTetris.Rendering
{
    public struct RgbColor
    {
        public byte R, G, B, A;

        public RgbColor(byte r, byte g, byte b, byte a = 255)
        {
            R = r; G = g; B = b; A = a;
        }

        public RgbColor Lighten(float factor)
        {
            return new RgbColor(
                (byte)Math.Min(255, R + (255 - R) * factor),
                (byte)Math.Min(255, G + (255 - G) * factor),
                (byte)Math.Min(255, B + (255 - B) * factor), A);
        }

        public RgbColor Darken(float factor)
        {
            return new RgbColor(
                (byte)(R * (1f - factor)),
                (byte)(G * (1f - factor)),
                (byte)(B * (1f - factor)), A);
        }
    }

    public static class TetrisColors
    {
        // Index 0 = empty/background, indices 1-7 match TetrominoType enum
        public static readonly RgbColor[] PieceColors =
        {
            new RgbColor(0,   0,   0),     // 0: empty
            new RgbColor(0,   255, 255),   // 1: I - cyan
            new RgbColor(255, 255, 0),     // 2: O - yellow
            new RgbColor(255, 0,   255),   // 3: T - magenta
            new RgbColor(0,   255, 0),     // 4: S - green
            new RgbColor(255, 0,   0),     // 5: Z - red
            new RgbColor(0,   0,   255),   // 6: J - blue
            new RgbColor(255, 165, 0),     // 7: L - orange
        };

        public static readonly string[] AnsiColors =
        {
            "\x1b[0m",    // 0 empty
            "\x1b[96m",   // 1 I  bright cyan
            "\x1b[93m",   // 2 O  bright yellow
            "\x1b[95m",   // 3 T  bright magenta
            "\x1b[92m",   // 4 S  bright green
            "\x1b[91m",   // 5 Z  bright red
            "\x1b[94m",   // 6 J  bright blue
            "\x1b[33m",   // 7 L  orange/brown
        };

        public const string AnsiReset  = "\x1b[0m";
        public const string AnsiBorder = "\x1b[37m";
        public const string AnsiGhost  = "\x1b[90m";

        public static readonly RgbColor Background   = new RgbColor(20, 20, 30);
        public static readonly RgbColor BorderColor   = new RgbColor(180, 180, 200);
        public static readonly RgbColor GhostColor    = new RgbColor(80, 80, 100);
        public static readonly RgbColor GridLineColor = new RgbColor(35, 35, 50);

        // Info panel colors
        public static readonly RgbColor InfoPanelBg    = new RgbColor(30, 30, 45);
        public static readonly RgbColor InfoPanelBorder = new RgbColor(100, 100, 130);
        public static readonly RgbColor InfoLabel       = new RgbColor(160, 160, 180);
        public static readonly RgbColor InfoValue       = new RgbColor(255, 255, 255);
        public static readonly RgbColor PausedBg        = new RgbColor(40, 40, 180);
        public static readonly RgbColor PausedText      = new RgbColor(255, 255, 255);
        public static readonly RgbColor GameOverBg      = new RgbColor(180, 30, 30);
        public static readonly RgbColor GameOverText    = new RgbColor(255, 255, 255);
    }
}
