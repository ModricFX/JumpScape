# Making levels

Each level is a plain text file in this folder named `Level<number>.txt` (`Level1.txt`, `Level2.txt`, ...).
The game finds them automatically and lists them in number order in the level picker. To add a level,
create `Level4.txt` and run the game again.

## Coordinates

Levels use a fixed 1920×1080 world, no matter how big the game window is.

- `x` goes from `0` (left edge) to `1920` (right edge).
- `y` grows **downwards**. `0` is the top of the first screen. Negative `y` values are above it; the camera
  scrolls up when the player climbs there.
- Every position is the **top-left corner** of the object.

Instead of a number you can write:

| Word           | Value  | Meaning                                                            |
| -------------- | ------ | ------------------------------------------------------------------ |
| `groundY`      | 1017   | The top of the ground. `groundY-200` is 200 pixels above the ground. |
| `groundLength` | 2880   | Wide enough for a platform to cover the whole screen.              |

## Lines

One object per line, written as `Type: values` with the values separated by commas.
Empty lines and lines starting with `#` are ignored.

| Line                                         | What it does                                                                                                  |
| -------------------------------------------- | ------------------------------------------------------------------------------------------------------------- |
| `PlayerSpawn: x, y`                          | Where the player starts.                                                                                      |
| `Platform: x, y, length, monster, crumbling` | A grass platform, 63 px thick. `monster` = `1` puts a frog on it. `crumbling` = `1` makes it break 3 s after the player lands on it (it comes back 5 s later). |
| `DoorPosition: x, y, locked`                 | The exit door. `locked` = `1` means the player needs the key.                                                 |
| `KeyPosition: x, y`                          | A key that unlocks the door. Add the line more than once for more keys.                                       |
| `Ghost: x, y, radius`                        | A ghost that chases the player when they come within `radius` pixels.                                         |
| `PowerUp: x, y, type`                        | A power-up, 44×44 px. `type` is `health`, `doubleJump` or `highJump`.                                         |

The first platform should normally be the ground: `Platform: -50,groundY,groundLength,0,0`.
A platform at `groundY-63` sits exactly on top of the ground. Anything between that and `groundY` overlaps
the grass and looks broken.

## Useful sizes

- The player is 91×148 px, jumps about 370 px high and can cover about 200 px sideways in one jump.
- Keep the spawn point clear of platforms, or the player gets pushed out of them when the level starts.
- The door is 123×140 px. To stand it on a platform at `groundY-N`, put the door at `groundY-(N+135)`.
- A frog is about 60 px wide and walks back and forth across its whole platform.
- To float a power-up where the player can grab it while standing on a platform at `groundY-N`, put it at
  about `groundY-(N+90)`.
- With high jump the player jumps about 580 px; with double jump, about 715 px in total.

## Example

This is `Level3.txt`: a staircase up to the key, then back down to the locked door.

```text
PlayerSpawn: 100,groundY-100
Platform: -50,groundY,groundLength,0,0
Platform: 200,groundY-63,250,0,0
Platform: 600,groundY-150,200,1,0
Platform: 1000,groundY-250,200,0,1
Platform: 1200,groundY-400,200,0,1
Platform: 1400,groundY-500,250,1,0
Platform: 1650,groundY-650,250,0,0
KeyPosition: 1800,groundY-785
DoorPosition: 1680,groundY-135,1
```

If a line can't be read, the game skips it and writes the reason to the debug output
(visible in the Visual Studio / VS Code debug console).
