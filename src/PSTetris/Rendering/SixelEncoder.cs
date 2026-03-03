using System.Collections.Generic;
using System.Text;

namespace PSTetris.Rendering
{
    public class SixelEncoder
    {
        private const int BandHeight = 6;

        private readonly Dictionary<int, RgbColor> _palette = new Dictionary<int, RgbColor>();
        private int _nextRegister;

        // Packed RGB -> register index
        private readonly Dictionary<int, int> _colorToRegister = new Dictionary<int, int>();

        public int RegisterColor(RgbColor color)
        {
            int packed = (color.R << 16) | (color.G << 8) | color.B;
            if (_colorToRegister.TryGetValue(packed, out int existing))
                return existing;

            int reg = _nextRegister++;
            _palette[reg] = color;
            _colorToRegister[packed] = reg;
            return reg;
        }

        public int GetRegister(RgbColor color)
        {
            int packed = (color.R << 16) | (color.G << 8) | color.B;
            return _colorToRegister.TryGetValue(packed, out int reg) ? reg : -1;
        }

        public string Encode(PixelBuffer buffer, int x, int y, int w, int h, int bgRegister = 0)
        {
            int bandStart = (y / BandHeight) * BandHeight;
            int bandEnd = ((y + h + BandHeight - 1) / BandHeight) * BandHeight;

            var sb = new StringBuilder(w * (bandEnd - bandStart) / 2);

            // DCS introducer
            sb.Append("\x1bPq");

            // Raster attributes: 1:1 aspect ratio, dimensions
            sb.Append('"');
            sb.Append("1;1;");
            sb.Append(w);
            sb.Append(';');
            sb.Append(bandEnd - bandStart);

            // Define color registers
            foreach (var kvp in _palette)
            {
                sb.Append('#');
                sb.Append(kvp.Key);
                sb.Append(";2;");
                sb.Append(kvp.Value.R * 100 / 255);
                sb.Append(';');
                sb.Append(kvp.Value.G * 100 / 255);
                sb.Append(';');
                sb.Append(kvp.Value.B * 100 / 255);
            }

            // Encode bands
            for (int bandY = bandStart; bandY < bandEnd; bandY += BandHeight)
            {
                bool firstColorInBand = true;

                foreach (var kvp in _palette)
                {
                    int reg = kvp.Key;
                    if (reg == bgRegister) continue;

                    bool colorUsed = false;
                    var bandData = new byte[w];

                    for (int col = 0; col < w; col++)
                    {
                        byte bits = 0;
                        for (int bit = 0; bit < BandHeight; bit++)
                        {
                            int py = bandY + bit;
                            int px = x + col;
                            if (py < buffer.Height && px < buffer.Width)
                            {
                                int pixelReg = GetRegister(buffer.GetPixel(px, py));
                                if (pixelReg == reg)
                                {
                                    bits |= (byte)(1 << bit);
                                    colorUsed = true;
                                }
                            }
                        }
                        bandData[col] = bits;
                    }

                    if (!colorUsed) continue;

                    if (!firstColorInBand)
                        sb.Append('$');
                    firstColorInBand = false;

                    sb.Append('#');
                    sb.Append(reg);

                    EncodeBandWithRLE(sb, bandData);
                }

                if (bandY + BandHeight < bandEnd)
                    sb.Append('-');
            }

            // ST terminator
            sb.Append("\x1b\\");

            return sb.ToString();
        }

        private static void EncodeBandWithRLE(StringBuilder sb, byte[] bandData)
        {
            int i = 0;
            while (i < bandData.Length)
            {
                byte val = bandData[i];
                char sixelChar = (char)(val + 0x3F);

                int run = 1;
                while (i + run < bandData.Length && bandData[i + run] == val)
                    run++;

                if (run >= 4)
                {
                    sb.Append('!');
                    sb.Append(run);
                    sb.Append(sixelChar);
                }
                else
                {
                    for (int j = 0; j < run; j++)
                        sb.Append(sixelChar);
                }

                i += run;
            }
        }

        public void ClearPalette()
        {
            _palette.Clear();
            _colorToRegister.Clear();
            _nextRegister = 0;
        }
    }
}
