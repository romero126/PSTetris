# Changelog

All notable changes to PSTetris will be documented in this file.

## [0.2.0] - 2026-03-06

### Added
- Restart option on game over screen — press R to restart or Q to quit
- Autoscaling for both renderers to fit the terminal window size
  - ANSI renderer scales cells by an integer factor based on available columns/rows
  - Sixel renderer dynamically computes pixel layout proportional to detected terminal dimensions (cell size 8–32px)
- Terminal pixel size detection via CSI 14t probe in `TerminalCapabilities`
- Alternate screen buffer support — game preserves terminal scrollback on exit

### Changed
- Sixel auto-detection restructured: DA1 probe runs first for reliable capability detection, environment-variable heuristics serve as fallback
- Windows Terminal (`WT_SESSION`) and VSCode (`VSCODE_GIT_ASKPASS_MAIN`) now correctly detected as Sixel-capable
- `IGameRenderer.RenderGameOver` returns `bool` to signal restart vs quit

## [0.1.0] - 2025-05-01

### Added
- Initial release
- Fully playable Tetris in the PowerShell console
- ANSI text renderer with Unicode block characters
- Sixel pixel renderer with bitmapped graphics
- Ghost piece preview
- Score, level, and line tracking
- Next piece display
- Pause support
