# JumpScape

![JumpScape logo](Content/Graphics/GameLogo/JumpScapeLogo.png)

JumpScape is a 2D platformer built with [MonoGame](https://monogame.net/) and C#. Climb from the ground up
through floating platforms to reach the exit door. Watch out for frogs and ghosts (or knock them out with a
glowing orb), and for platforms that crumble under your feet. Some doors are locked, so find the key first.

## Features

- Four levels, plus a JSON level format for making your own, with autocomplete and error checking in
  VS Code ([Levels/README.md](Levels/README.md)).
- Levels can have several areas joined by glowing portal doors. Level 4 hides its key in a dark red nether.
- Frogs that patrol platforms, ghosts that chase you, crumbling platforms, and locked doors with keys.
- Throw glowing orbs at enemies by clicking where you want them to go.
- Pip, a friendly explorer, explains the controls in Level 1 and gives tips in the other levels.
- Race your best run: after you finish a level, a see-through ghost replays your fastest run while a
  timer shows your time and your best. Best times also appear in the level picker.
- Power-ups: hearts that heal you, plus double jump and high jump orbs that last 15 seconds.
- Three hearts of health, knockback, and a short moment of invincibility after every hit.
- Sounds that get quieter with distance and come from the left or right.
- Starts in fullscreen. Settings for game volume, music volume, mute, a 30 or 60 FPS limit, and fullscreen.
- Your progress and settings are saved. **Play** continues with the next level you haven't finished, and
  finished levels get a star in the level picker.
- Wooden menus throughout: pause, game over, and a level complete screen with your time and a
  **Next level** button.
- A soft, blurred forest behind each level that scrolls slowly as you climb, plus dust puffs, a little
  bounce when walking, and hints like **[E] Open** next to doors.
- Levels are designed for 1920×1080 and scaled to fit any window. A window of a different shape shows a bit
  more of the world instead of black bars.

## Requirements

- The [.NET SDK](https://dotnet.microsoft.com/download), version 8.0 or newer.
- Windows, macOS or Linux.
- The **Arial** font, which the build uses to create the game's font. Windows and macOS already have it.
  On Linux, install Microsoft's core fonts first (for example `sudo apt install ttf-mscorefonts-installer`).

You don't need to install MonoGame separately. It comes from NuGet, and its content tools are restored
automatically the first time you build.

## How to run

From the repository folder:

```bash
dotnet run
```

The first run takes a minute or two because it downloads packages and builds the images and sounds.
Later runs start almost immediately.

You can also open the folder in **VS Code** and press F5, or open `JumpScape.sln` in **Visual Studio** or
**Rider** and press Run.

### Making a copy to share

To build a version that runs on a computer without .NET installed:

```bash
dotnet publish JumpScape.csproj -c Release -r win-x64 --self-contained -o publish
```

Zip the `publish` folder and share it; players start `JumpScape.exe`. For other systems, replace `win-x64`
with `osx-arm64` (Apple Silicon Mac), `osx-x64` (Intel Mac) or `linux-x64`.

## Controls

| Action                 | Keys                                    |
| ---------------------- | --------------------------------------- |
| Move                   | Arrow keys or A / D                     |
| Jump                   | Space, W or Up arrow                    |
| Use the door, talk     | E                                       |
| Select inventory slot  | 1, 2, 3                                 |
| Drop the selected key  | Q                                       |
| Throw an orb           | Left mouse button (aim with the mouse)  |
| Pause / back           | Esc                                     |
| Menus                  | Arrow keys or W / S, Enter or Space, or the mouse |

## How to play

- Reach the door at the end of the level and press **E** to open it, then **E** again to go through. The
  level complete screen shows your time and lets you go to the next level, play again or go back to the menu.
- A locked door needs a key. Pick up the key, select its inventory slot with **1–3**, and press **E** next
  to the door to unlock it. Each locked door uses up one key. Press **Q** to drop the selected key again.
- Glowing doors are portals to another part of the level. Open one with **E** and press **E** again to go
  through. The portal on the other side brings you back.
- Frogs take a whole heart if they walk into you and half a heart if you bump into them from behind.
  Ghosts take half a heart. After a hit you flash orange and can't be hurt for a moment.
- Click to throw an orb towards the mouse. One orb defeats a frog or a ghost. Orbs stop when they hit a
  platform, and you can throw about two per second.
- Power-ups float in the level; touch one to collect it:

  | Power-up         | Looks like  | Effect                                                         |
  | ---------------- | ----------- | -------------------------------------------------------------- |
  | Heart            | A red heart | Gives back one heart. It stays put if your health is full.     |
  | Double jump      | Green orb   | Press jump again in mid-air to jump a second time. Lasts 15 s. |
  | High jump        | Red orb     | Jump about one and a half times as high. Lasts 15 s.           |

  Active power-ups show under your hearts, with a bar for the time left.
- Crumbling platforms start shaking when you land on them, break after three seconds and come back five
  seconds later.
- Press **E** next to Pip to talk. **E** (or Enter, Space or a click) shows the next line, and **Esc** ends
  the conversation. The game waits while you talk.
- Your time is shown at the top. Finish faster than before to set a new best; the ghost of your best run
  races you next time. You can turn the ghost off under **Settings → Gameplay**.

## Saved data

Settings and progress are saved as small files: `settings.json`, `progress.json`, and one
`best-run-levelN.bin` per level with your fastest run:

- Windows: `%APPDATA%\JumpScape\`
- macOS and Linux: `~/.config/JumpScape/`

Delete them to reset the game.

## Project structure

```text
Program.cs                Entry point
Source/
  JumpScapeGame.cs        Window, music, switching between screens, and drawing
  Core/                   Assets, input, settings, save files and screen scaling
  Gameplay/               The level being played: player, platforms, enemies, doors, items, effects, backdrop
  Menus/                  Main menu, level picker, settings, shared wooden UI pieces, and the panel menus
                          used for pause, game over and level complete
Content/
  Content.mgcb            List of every image, sound and font the game uses
  Fonts/ Graphics/ Sounds/
Levels/                   Level files (LevelN.json), their schema, and a guide (Levels/README.md)
```

The images, sounds and font are built by the MonoGame content pipeline. If you add a new asset, add it to
`Content/Content.mgcb`, either by hand or with the MGCB Editor (`dotnet mgcb-editor Content/Content.mgcb`).

## License

This project is licensed under the MIT License.
