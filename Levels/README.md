# Making levels

Each level is a JSON file in this folder named `Level<number>.json` (`Level1.json`, `Level2.json`, ...).
The game finds them automatically and lists them in number order in the level picker. To add a level,
copy an existing one to `Level13.json`, change it, and run the game again. A level unlocks once the one
before it is finished, or straight away with **Settings → Unlock all levels**.

Start every file with `"$schema": "level.schema.json"`. Editors like VS Code then autocomplete the settings,
explain each one when you hover over it, and underline mistakes while you type.

If a level has a mistake, the game shows what is wrong and on which line when you try to play it, for
example: *Level13.json, line 14: "lenght" isn't a setting a platform can have. Check the spelling.*

## A small level

```json
{
  "$schema": "level.schema.json",
  "name": "Frog Hill",
  "description": "Two steps and a frog. The level picker shows this text.",
  "medals": { "gold": 10, "silver": 15, "bronze": 25 },
  "spawn": { "x": 40, "y": "groundY-100" },
  "areas": [
    {
      "name": "main",
      "platforms": [
        { "x": -50, "y": "groundY", "length": "groundLength" },
        { "x": 300, "y": "groundY-150", "length": 200 },
        { "x": 600, "y": "groundY-300", "length": 200, "monster": true }
      ],
      "doors": [
        { "x": 1700, "y": "groundY-135" }
      ],
      "gems": [
        { "x": 680, "y": "groundY-640" }
      ]
    }
  ]
}
```

Comments (`// like this`) and a comma after the last item are allowed, to make editing easier.

## Coordinates

Levels use a fixed 1920×1080 world, no matter how big the game window is.

- `x` goes from `0` (left edge) to `1920` (right edge).
- `y` grows **downwards**. `0` is the top of the first screen. Negative `y` values are above it; the camera
  scrolls up when the player climbs there.
- Every position is the **top-left corner** of the object.

Any number can also be written as text using these words, optionally followed by `+N` or `-N`:

| Word           | Value  | Meaning                                                              |
| -------------- | ------ | -------------------------------------------------------------------- |
| `groundY`      | 1017   | The top of the ground. `"groundY-200"` is 200 pixels above the ground. |
| `groundLength` | 2880   | Wide enough for a platform to cover the whole screen.                |

## What a level contains

| Setting       | What it is                                                                                     |
| ------------- | ---------------------------------------------------------------------------------------------- |
| `spawn`       | Where the player starts: `{ "x": ..., "y": ... }`.                                              |
| `startArea`   | Name of the area the player starts in. Optional; the first area if left out.                   |
| `areas`       | The parts of the level. Most levels have one area, called `main`.                               |
| `name`        | The level's title, shown when it starts and in the level picker. Optional.                     |
| `description` | A sentence or two about the level, shown in the level picker. Optional.                        |
| `medals`      | Target times in seconds: `{ "gold": 20, "silver": 30, "bronze": 50 }`. Optional.               |

Each area has a `name` and lists of things. Every list is optional.

| List        | Each item                                              | Notes |
| ----------- | ------------------------------------------------------ | ----- |
| `platforms` | `x`, `y`, `length`, and optionally `"monster": true`, `"crumbling": true` or `"moveTo"` | Grass platforms, 63 px thick. A monster puts a frog on it. A crumbling platform breaks 3 s after the player lands on it and comes back 5 s later. The first platform is usually the ground: `{ "x": -50, "y": "groundY", "length": "groundLength" }`. See [Moving platforms](#moving-platforms). |
| `doors`     | `x`, `y`, optionally `"locked": true`                  | Exit doors. Going through one finishes the level. A locked door needs a key, and each locked door uses up one key. |
| `portals`   | `x`, `y`, `name`, `leadsTo`, optionally `"locked": true` | Glowing doors that take the player to the portal called `leadsTo` (see below). |
| `keys`      | `x`, `y`                                               | Keys for locked doors and portals. |
| `ghosts`    | `x`, `y`, `radius`                                     | Ghosts that chase the player when they come within `radius` pixels. |
| `powerUps`  | `x`, `y`, `type`                                       | `type` is `"health"`, `"doubleJump"` or `"highJump"`. |
| `npcs`      | `x`, `y`, `name`, `lines`                              | Friendly characters the player talks to with E. `lines` is a list of what they say, one entry at a time. Place them anywhere above a platform; they stand on the platform below. |
| `checkpoints` | `x`, `y`                                             | Flags, 64×120 px. After touching one, the player can come back to it after dying. To stand one on a platform at `groundY-N`, put it at `groundY-(N+120)`. |
| `spikes`    | `x`, `y`, `length`                                     | A row of spikes, 30 px tall and 30 px per spike. Touching them costs a heart. To put them on a platform at `groundY-N`, use `groundY-(N+30)`. |
| `gems`      | `x`, `y`                                               | Hidden gems, 36×36 px. They count when the level is finished. The player can reach one about 330 px above the platform they stand on by jumping. |
| `bouncePads` | `x`, `y`                                              | Springs, 80×30 px, that launch the player about 600 px up. To put one on a platform at `groundY-N`, use `groundY-(N+30)`. The player steers while flying, so put the platform to land on to one side, not straight above. |

A platform at `groundY-63` sits exactly on top of the ground. Anything between that and `groundY` overlaps
the grass and looks broken.

## Areas and portals

A level can have more than one area, for example a normal world and a "nether" with a dark red sky. Areas
can set two colors:

- `sky` is the background color and `tint` colors the platforms. Both are optional.
- Colors can be a hex code like `"#4A0D12"` or a color name like `"DarkRed"` or `"CornflowerBlue"`. Any
  MonoGame color name works.

Portals come in pairs. Give each one a `name`, and set its `leadsTo` to the name of the other one:

```json
"areas": [
  {
    "name": "main",
    "portals": [ { "x": 1010, "y": "groundY-585", "name": "netherGate", "leadsTo": "overworldGate" } ]
  },
  {
    "name": "nether", "sky": "#4A0D12", "tint": "Salmon",
    "portals": [ { "x": 100, "y": "groundY-135", "name": "overworldGate", "leadsTo": "netherGate" } ]
  }
]
```

The player opens a portal with E and goes through with E again. They come out standing in front of the
other portal, which is already open so they can go straight back. Health, keys and power-ups come with
them. Each area needs its own ground platform. If a portal's `leadsTo` doesn't exist, it works like an exit
door.

## Moving platforms

Give a platform a `moveTo` position and it glides back and forth between where it starts and there,
slowing down at each end. `moveTime` is how many seconds one way takes (3 if left out):

```json
{ "x": 550, "y": "groundY-450", "length": 160, "moveTo": { "x": 1150, "y": "groundY-450" }, "moveTime": 3 }
```

A platform that moves up and down works as a lift. The player rides along while standing on it, and a dotted
line shows the path. Moving platforms can crumble, but they can't have a monster. Keys, gems and power-ups
stay where they are, so don't put them where only a moving platform can reach. All moving platforms in an
area start together when the level starts.

## Characters

```json
"npcs": [
  {
    "x": 230, "y": "groundY-300", "name": "Pip",
    "lines": [
      "Hi, I'm Pip! Walk with the arrow keys or A and D.",
      "Climb the platforms to the door at the top."
    ]
  }
]
```

The text can use accented letters (č, š, ž, é, ...) and curly quotes.

Players can choose their own keys, so name keys with these placeholders instead of writing "E" or "Space":
`{left}`, `{right}`, `{jump}`, `{use}` and `{drop}`. For example, `"Press {use} at the door."` shows as
"Press E at the door." with the usual keys. The level's `description` can use them too.

## Useful sizes

- The player is 91×148 px, jumps about 370 px high and can cover about 200 px sideways in one jump.
  A gap of 250 px between platforms at the same height is a long jump; 300 px is too far.
- Keep the spawn point clear of platforms, or the player gets pushed out of them when the level starts.
- The player bumps their head on platforms above them. A platform less than about 600 px above another
  one should not hang over the edge the player jumps from, or the jump hits it and falls short.
- A platform less than 150 px above another one (counting from its top) blocks walking along the one
  below; the player has to jump onto it, like a step.
- Don't put spikes where a player who falls has no way out except through them.
- The door is 123×140 px. To stand it on a platform at `groundY-N`, put the door at `groundY-(N+135)`.
- A frog is about 60 px wide and walks back and forth across its whole platform.
- To float a power-up where the player can grab it while standing on a platform at `groundY-N`, put it at
  about `groundY-(N+90)`.
- With high jump the player jumps about 580 px; with double jump, about 715 px in total.

## Checking a level

The automated player in `Tests` can play your level from start to finish, collecting every gem, and tell
you which jumps are tight:

```bash
dotnet run --project Tests -- --levels 13
```

If it can't finish, it lists every part of every platform and where it could get to from there. A picture
of the whole level helps too:

```bash
dotnet run --project Tests -- --overview
```

## Best runs

The game saves the fastest run of each level and replays it as a ghost. Changing what is in a level resets
its best run, because the ghost would no longer match. Changing only spacing, comments, the `name` or the
`description` keeps it.

## Examples

- `Level1.json` is the simplest: a staircase, one frog and Pip.
- `Level4.json` has a key at the top and a locked door on the ground.
- `Level5.json` has bounce pads.
- `Level8.json` shows areas and portals: the key for the exit is in the nether.
- `Level9.json` has two keys and a locked portal to a second area.
- `Level10.json` has moving platforms: two ferries and a lift.
- `Level11.json` has checkpoints, spikes on a platform and gems high above the climb.
