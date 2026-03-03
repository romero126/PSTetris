# How to Play PSTetris

## Objective

Stack falling pieces (tetrominoes) to fill complete horizontal lines. Each complete line is cleared from the board, earning you points. The game ends when pieces stack up to the top of the board.

## The Board

The playing field is **10 columns wide** and **20 rows tall**. Pieces enter from the top and fall towards the bottom.

## The Pieces

There are seven tetrominoes, each made of four cells:

| Name | Shape | Color |
|------|-------|-------|
| **I** | Four in a line `████` | Cyan |
| **O** | 2×2 square `██`/`██` | Yellow |
| **T** | T-shape | Magenta |
| **S** | S-shape (right-slant) | Green |
| **Z** | Z-shape (left-slant) | Red |
| **J** | J-shape | Blue |
| **L** | L-shape | Orange |

## Controls

| Key | Action |
|-----|--------|
| `←` or `A` | Move piece left |
| `→` or `D` | Move piece right |
| `↑` or `W` | Rotate clockwise |
| `Z` or `X` | Rotate counter-clockwise |
| `↓` or `S` | Soft drop |
| `Space` | Hard drop |
| `P` | Pause / Resume |
| `Esc` or `Q` | Quit |

## Movement

**Moving left/right:** Slides the active piece one column in the chosen direction. The piece cannot move through the board walls or through locked pieces.

**Rotating:** Turns the piece 90 degrees. If there is not enough room to rotate in place, the game tries small horizontal shifts (wall kicks) to find a valid position. If no valid position exists, the rotation is cancelled.

## Dropping

**Soft drop (`↓` / `S`):** Moves the piece down one row instantly, earning **1 point per cell** dropped. The fall timer resets, giving you time to fine-tune horizontal position.

**Hard drop (`Space`):** Instantly slams the piece to the lowest valid row, earning **2 points per cell** dropped, and locks it immediately. Use this for speed or when the ghost piece is already lined up.

## Ghost Piece

A faint outline (darker shade of the active piece) shows exactly where the piece will land if dropped straight down. Use it to aim your placements without needing to count rows.

## Next Piece Preview

The panel on the right shows the **next piece** so you can plan ahead.

## Locking

A piece **locks** (becomes part of the fixed board) when it can no longer move down — either from resting on the floor or on top of another locked piece. Once locked:

1. Any fully filled horizontal rows are cleared.
2. Points are added to your score.
3. The next piece spawns at the top.

## Line Clears and Scoring

| Lines cleared | Points |
|---------------|--------|
| 1 (Single) | 100 × level |
| 2 (Double) | 300 × level |
| 3 (Triple) | 500 × level |
| 4 (Tetris) | 800 × level |

Clearing four lines at once (a **Tetris**) is the most efficient way to score. The I-piece is the only piece that can produce a Tetris on a full board.

## Levels and Speed

The game track how many total lines you have cleared:

```
Level = (total lines ÷ 10) + 1
```

Every 10 lines the level increases by 1, making pieces fall faster. The fall interval formula is:

```
Fall interval (ms) = max(80, 800 - (level - 1) × 70)
```

| Level | Lines needed | Fall interval |
|-------|--------------|---------------|
| 1 | 0 | 800 ms |
| 2 | 10 | 730 ms |
| 3 | 20 | 660 ms |
| 5 | 40 | 520 ms |
| 8 | 70 | 310 ms |
| 10 | 90 | 170 ms |
| 11+ | 100+ | 80 ms |

At level 11 the game reaches maximum speed (80 ms per fall step, roughly 12.5 rows per second).

## Pausing

Press `P` to pause the game. The board is still visible but no input is processed (except `P` to resume or `Esc`/`Q` to quit).

## Game Over

The game ends when a newly spawned piece cannot be placed at the top of the board — the stack has reached the ceiling. Your final score, level, and line count are displayed.

## Tips

- **Clear multiple lines at once.** Doubles and Triples score much better than Singles.
- **Keep the board flat.** Avoid tall, irregular stacks. Isolated holes are very hard to fill later.
- **Use the I-piece for Tetrominoes.** Leave a one-column gap on one side and fill it with the I-piece for maximum points.
- **Hard drop when safe.** If the ghost piece is already positioned correctly, use `Space` to lock immediately and earn bonus points.
- **Plan one piece ahead.** The next-piece preview is your most valuable tool — always know what is coming.
- **Wall kicks help you.** If a rotation seems blocked, try it anyway — the game shifts the piece horizontally to make it fit.
