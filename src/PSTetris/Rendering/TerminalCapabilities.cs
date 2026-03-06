using System;
using System.Text;
using System.Threading;

namespace PSTetris.Rendering
{
    public static class TerminalCapabilities
    {
        public static bool DetectSixelSupport()
        {
            // Try DA1 probe first — the most reliable method since it asks
            // the terminal directly whether it supports Sixel (attribute 4).
            if (!Console.IsInputRedirected && !Console.IsOutputRedirected)
            {
                bool? probeResult = ProbeDA1();
                if (probeResult.HasValue)
                    return probeResult.Value;
            }

            // DA1 probe was inconclusive (timeout / no response).
            // Fall back to environment-variable heuristics.

            // Known Sixel-capable terminals
            string termProgram = Environment.GetEnvironmentVariable("TERM_PROGRAM") ?? "";

            if (termProgram.IndexOf("mlterm", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (termProgram.IndexOf("mintty", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (termProgram.IndexOf("foot", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (termProgram.IndexOf("WezTerm", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (termProgram.IndexOf("contour", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            // Windows Terminal supports Sixel (since v1.22)
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WT_SESSION")))
                return true;

            // VSCode integrated terminal supports Sixel (xterm.js)
            if (termProgram.IndexOf("vscode", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("VSCODE_GIT_ASKPASS_MAIN")))
                return true;

            // Known Sixel-incapable terminals
            if (termProgram.IndexOf("Apple_Terminal", StringComparison.OrdinalIgnoreCase) >= 0)
                return false;

            return false;
        }

        /// <summary>
        /// Detects terminal dimensions in both characters and pixels.
        /// Pixel dimensions are obtained via CSI 14t probe; falls back to estimation.
        /// </summary>
        public static void DetectTerminalSize(out int cols, out int rows,
                                               out int pixelW, out int pixelH)
        {
            cols = Console.WindowWidth;
            rows = Console.WindowHeight;

            pixelW = 0;
            pixelH = 0;

            if (!Console.IsInputRedirected && !Console.IsOutputRedirected)
            {
                ProbePixelSize(out pixelW, out pixelH);
            }

            // Fallback: estimate from character dimensions
            if (pixelW <= 0 || pixelH <= 0)
            {
                pixelW = cols * 8;
                pixelH = rows * 16;
            }
        }

        /// <summary>
        /// Probes terminal pixel size using CSI 14t.
        /// Response format: ESC[4;&lt;height&gt;;&lt;width&gt;t
        /// </summary>
        private static void ProbePixelSize(out int pixelW, out int pixelH)
        {
            pixelW = 0;
            pixelH = 0;
            try
            {
                while (Console.KeyAvailable)
                    Console.ReadKey(intercept: true);

                Console.Write("\x1b[14t");

                var response = new StringBuilder();
                var deadline = DateTime.UtcNow.AddMilliseconds(500);

                while (DateTime.UtcNow < deadline)
                {
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(intercept: true);
                        response.Append(key.KeyChar);
                        if (key.KeyChar == 't' && response.Length > 2)
                            break;
                    }
                    else
                    {
                        Thread.Sleep(10);
                    }
                }

                string resp = response.ToString();
                // Expected: ESC[4;<height>;<width>t
                int idx4 = resp.IndexOf('4');
                int idxT = resp.LastIndexOf('t');
                if (idx4 < 0 || idxT <= idx4) return;

                string body = resp.Substring(idx4 + 1, idxT - idx4 - 1);
                // body should be ";<height>;<width>"
                string[] parts = body.Split(';');
                if (parts.Length >= 3 &&
                    int.TryParse(parts[1].Trim(), out int h) &&
                    int.TryParse(parts[2].Trim(), out int w))
                {
                    pixelH = h;
                    pixelW = w;
                }
            }
            catch
            {
                // Probe failed, leave at 0
            }
        }

        /// <summary>
        /// Sends a DA1 query (ESC[c) and checks for Sixel attribute (4).
        /// Returns true/false if a definitive answer is obtained,
        /// or null if the probe was inconclusive (no response / timeout).
        /// </summary>
        private static bool? ProbeDA1()
        {
            try
            {
                // Flush pending input
                while (Console.KeyAvailable)
                    Console.ReadKey(intercept: true);

                // Send DA1 query
                Console.Write("\x1b[c");

                // Read response with timeout
                var response = new StringBuilder();
                var deadline = DateTime.UtcNow.AddMilliseconds(500);

                while (DateTime.UtcNow < deadline)
                {
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(intercept: true);
                        response.Append(key.KeyChar);

                        if (key.KeyChar == 'c' && response.Length > 2)
                            break;
                    }
                    else
                    {
                        Thread.Sleep(10);
                    }
                }

                string resp = response.ToString();

                // No response at all — probe inconclusive
                if (resp.Length == 0)
                    return null;

                int qMark = resp.IndexOf('?');
                int cEnd = resp.LastIndexOf('c');
                if (qMark < 0 || cEnd <= qMark) return false;

                string attrs = resp.Substring(qMark + 1, cEnd - qMark - 1);
                foreach (string attr in attrs.Split(';'))
                {
                    if (int.TryParse(attr.Trim(), out int val) && val == 4)
                        return true;
                }

                return false;
            }
            catch
            {
                return null;
            }
        }
    }
}
