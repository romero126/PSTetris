using System.Management.Automation;
using PSTetris.Rendering;

namespace PSTetris
{
    /// <summary>
    /// <para type="synopsis">Starts an interactive Tetris game in the current console window.</para>
    /// <para type="description">
    /// Launches a fully playable Tetris game rendered directly in the PowerShell console.
    /// Requires an interactive console (not PowerShell ISE or a redirected session).
    /// Use the -Renderer parameter to select graphics mode:
    ///   auto  - detect Sixel support; fall back to ANSI text (default)
    ///   sixel - force Sixel pixel graphics
    ///   ansi  - force ANSI text rendering
    /// </para>
    /// <example>
    ///   <code>Start-Tetris</code>
    /// </example>
    /// <example>
    ///   <code>Start-Tetris -Renderer sixel</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsLifecycle.Start, "Tetris")]
    public class StartTetrisCmdlet : PSCmdlet
    {
        [Parameter(Position = 0)]
        [ValidateSet("auto", "sixel", "ansi")]
        public string Renderer { get; set; } = "auto";

        protected override void ProcessRecord()
        {
            IGameRenderer renderer;

            switch (Renderer.ToLowerInvariant())
            {
                case "sixel":
                    renderer = new SixelRenderer();
                    break;
                case "ansi":
                    renderer = new AnsiRenderer();
                    break;
                default:
                    bool sixelSupported = TerminalCapabilities.DetectSixelSupport();
                    renderer = sixelSupported ? (IGameRenderer)new SixelRenderer()
                                              : new AnsiRenderer();
                    if (sixelSupported)
                        WriteVerbose("Sixel graphics detected. Using pixel renderer.");
                    else
                        WriteVerbose("Sixel not detected. Using text renderer.");
                    break;
            }

            var game = new TetrisGame(renderer);
            game.Run();
        }
    }
}
