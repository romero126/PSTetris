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
