using System;

namespace PSTetris.Rendering
{
    public class PixelBuffer
    {
        public int Width { get; }
        public int Height { get; }

        private readonly byte[] _pixels; // RGBA interleaved

        private int _dirtyMinX, _dirtyMinY, _dirtyMaxX, _dirtyMaxY;
        private bool _hasDirty;

        public PixelBuffer(int width, int height)
        {
            Width = width;
            Height = height;
            _pixels = new byte[width * height * 4];
            ResetDirty();
        }

        public void SetPixel(int x, int y, RgbColor color)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) return;
            int idx = (y * Width + x) * 4;
            _pixels[idx]     = color.R;
            _pixels[idx + 1] = color.G;
            _pixels[idx + 2] = color.B;
            _pixels[idx + 3] = color.A;
            ExpandDirty(x, y);
        }

        public RgbColor GetPixel(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return new RgbColor(0, 0, 0, 0);
            int idx = (y * Width + x) * 4;
            return new RgbColor(_pixels[idx], _pixels[idx + 1], _pixels[idx + 2], _pixels[idx + 3]);
        }

        public void FillRect(int x, int y, int w, int h, RgbColor color)
        {
            int x2 = Math.Min(x + w, Width);
            int y2 = Math.Min(y + h, Height);
            x = Math.Max(0, x);
            y = Math.Max(0, y);

            for (int py = y; py < y2; py++)
            {
                int rowBase = py * Width * 4;
                for (int px = x; px < x2; px++)
                {
                    int idx = rowBase + px * 4;
                    _pixels[idx]     = color.R;
                    _pixels[idx + 1] = color.G;
                    _pixels[idx + 2] = color.B;
                    _pixels[idx + 3] = color.A;
                }
            }

            if (x < x2 && y < y2)
            {
                ExpandDirty(x, y);
                ExpandDirty(x2 - 1, y2 - 1);
            }
        }

        public bool HasDirty => _hasDirty;
        public int DirtyMinX => _dirtyMinX;
        public int DirtyMinY => _dirtyMinY;
        public int DirtyMaxX => _dirtyMaxX;
        public int DirtyMaxY => _dirtyMaxY;

        public void ResetDirty()
        {
            _hasDirty = false;
            _dirtyMinX = Width;
            _dirtyMinY = Height;
            _dirtyMaxX = 0;
            _dirtyMaxY = 0;
        }

        public void MarkFullDirty()
        {
            _dirtyMinX = 0;
            _dirtyMinY = 0;
            _dirtyMaxX = Width - 1;
            _dirtyMaxY = Height - 1;
            _hasDirty = true;
        }

        public byte[] RawPixels => _pixels;

        private void ExpandDirty(int x, int y)
        {
            _hasDirty = true;
            if (x < _dirtyMinX) _dirtyMinX = x;
            if (y < _dirtyMinY) _dirtyMinY = y;
            if (x > _dirtyMaxX) _dirtyMaxX = x;
            if (y > _dirtyMaxY) _dirtyMaxY = y;
        }
    }
}
