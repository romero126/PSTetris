using System;
using System.Text;
using System.Threading;

namespace PSTetris.Rendering
{
    public static class TerminalCapabilities
    {
        public static bool DetectSixelSupport()
        {
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

            // Known Sixel-incapable terminals
            if (termProgram.IndexOf("vscode", StringComparison.OrdinalIgnoreCase) >= 0)
                return false;
            if (termProgram.IndexOf("Apple_Terminal", StringComparison.OrdinalIgnoreCase) >= 0)
                return false;

            // Windows Terminal does not support Sixel
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WT_SESSION")))
                return false;

            // DA1 probe
            return ProbeDA1();
        }

        private static bool ProbeDA1()
        {
            try
            {
                if (Console.IsInputRedirected || Console.IsOutputRedirected)
                    return false;

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
                return false;
            }
        }
    }
}
