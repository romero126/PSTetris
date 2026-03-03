# Architecture

PSTetris is a binary PowerShell module written in C# targeting .NET Standard 2.0. The module exposes a single cmdlet, `Start-Tetris`, which launches an interactive Tetris game rendered directly in the host console.

## Project Layout

```
src/PSTetris/
├── PSTetris.csproj             # SDK-style project, netstandard2.0
├── PSTetris.psd1               # Module manifest (PSGallery metadata)
├── StartTetrisCmdlet.cs        # Cmdlet entry point
├── TetrisGame.cs               # Core game loop and rules
├── Tetromino.cs                # Piece definitions and rotation
└── Rendering/
    ├── IGameRenderer.cs        # Renderer contract
    ├── GameState.cs            # Immutable state snapshot for renderers
    ├── AnsiRenderer.cs         # ANSI/Unicode text renderer
    ├── SixelRenderer.cs        # Sixel pixel-graphics renderer
    ├── SixelEncoder.cs         # Sixel protocol encoding + RLE
    ├── BitmapFont.cs           # 5×7 bitmap font for Sixel text
    ├── PixelBuffer.cs          # Off-screen pixel buffer with dirty tracking
    ├── TerminalCapabilities.cs # Sixel support detection
    └── TetrisColors.cs         # Shared color definitions (RGB + ANSI)
```

## Component Overview

### Entry Point — `StartTetrisCmdlet`

The `[Cmdlet(VerbsLifecycle.Start, "Tetris")]` class is the only public surface of the module. It accepts a `-Renderer` parameter (`auto`, `ansi`, `sixel`), resolves the renderer implementation, and hands control to `TetrisGame.Run()`.

### Game Engine — `TetrisGame`

Owns the 10×20 board grid, piece spawning (bag of `TetrominoType` values), collision detection, wall-kick rotation, line clearing, scoring, level progression, and the main game loop. The loop reads console key input, applies gravity on a timer, and pushes an immutable `GameState` snapshot to the renderer each frame.

### Piece Model — `Tetromino`

Stores the seven standard Tetromino shapes (I, O, T, S, Z, J, L) with all four rotation states encoded as relative cell offsets. The I-piece uses a 4×4 bounding box; all others use 3×3. Provides `GetAbsoluteCells()` for board-space positions and `Clone()` for speculative move validation.

### Rendering Contract — `IGameRenderer`

```
Initialize(boardWidth, boardHeight)
RenderFrame(GameState)
RenderInfo(GameState)
RenderGameOver(GameState)
Cleanup()
```

The game engine only ever interacts with this interface, keeping game logic fully decoupled from presentation.

### ANSI Renderer — `AnsiRenderer`

Renders the board using ANSI SGR color codes and Unicode block characters (`█` for filled cells, `░` for ghost cells). Draws a box-drawing border, a right-side info panel with score/level/lines/next-piece, and overlay text for pause and game-over states. Works in any terminal that supports basic ANSI colors.

### Sixel Renderer — `SixelRenderer`

Renders the board as pixel graphics via the Sixel terminal protocol. Each cell is 16×16 pixels with 3D beveled shading (lighten/darken on edges). Uses `PixelBuffer` for off-screen compositing, `SixelEncoder` for protocol output with RLE compression, and `BitmapFont` to draw text labels into the pixel buffer. Tracks dirty rectangles to minimize re-encoding between frames.

Supporting types:

| Type | Role |
|------|------|
| `PixelBuffer` | RGBA pixel array with bounds-checked drawing and dirty-rect tracking |
| `SixelEncoder` | Converts pixel regions to Sixel escape sequences; manages color palette registers |
| `BitmapFont` | 5×7 glyph definitions for digits, letters, and punctuation; scalable rendering |
| `TetrisColors` | Central palette — `RgbColor` struct with `Lighten`/`Darken` helpers, piece colors indexed by `TetrominoType` |

### Terminal Detection — `TerminalCapabilities`

`DetectSixelSupport()` checks the `TERM_PROGRAM` environment variable against known terminal lists, then falls back to a DA1 (Device Attributes) escape-sequence probe with a 500 ms timeout. Returns `false` immediately inside Windows Terminal or VS Code.

## Data Flow

```
StartTetrisCmdlet
  │
  ├─ TerminalCapabilities.DetectSixelSupport()
  │
  ├─ new AnsiRenderer() / new SixelRenderer()
  │
  └─ TetrisGame.Run()
       │
       loop {
         read input → move / rotate / drop
         apply gravity timer
         clear lines, update score
         build GameState snapshot
         renderer.RenderFrame(state)
         renderer.RenderInfo(state)
       }
       │
       renderer.RenderGameOver(state)
       renderer.Cleanup()
```

## Build and Publish

```powershell
.\Build-Module.ps1            # compiles + stages to output\PSTetris\
.\Publish-Module.ps1 -NuGetApiKey $key   # pushes to PSGallery
```

`Build-Module.ps1` runs `dotnet build` in Release mode and copies the DLL and manifest into `output\PSTetris\`. `Publish-Module.ps1` calls `Publish-Module` against the staged directory.
