# JumpScape

![JumpScape logo](Content/Graphics/GameLogo/JumpScapeLogo.png)

JumpScape is a 2D platformer built with [MonoGame](https://monogame.net/) and C#. Climb from the ground up
through floating platforms to reach the exit door. Watch out for frogs and ghosts, and for platforms that
crumble under your feet. Some doors are locked, so find the key first.

## Features

- Three levels, plus a simple text format for making your own ([Levels/README.md](Levels/README.md)).
- Frogs that patrol platforms, ghosts that chase you, crumbling platforms, and locked doors with keys.
- Three hearts of health, knockback, and a short moment of invincibility after every hit.
- Sounds that get quieter with distance and come from the left or right.
- Starts in fullscreen. Settings for game volume, music volume, mute, a 30 or 60 FPS limit, and fullscreen.
- Your progress and settings are saved. **Play** continues with the next level you haven't finished.
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
| Use the door           | E                                       |
| Select inventory slot  | 1, 2, 3                                 |
| Drop the selected key  | Q                                       |
| Pause / back           | Esc                                     |
| Menus                  | Arrow keys or W / S, Enter or Space, or the mouse |

## How to play

- Reach the door at the end of the level and press **E** to open it, then **E** again to go through.
- A locked door needs a key. Pick up the key, select its inventory slot with **1–3**, and press **E** next
  to the door to unlock it. Press **Q** to drop the selected key again.
- Frogs take a whole heart if they walk into you and half a heart if you bump into them from behind.
  Ghosts take half a heart. After a hit you flash orange and can't be hurt for a moment.
- Crumbling platforms start shaking when you land on them, break after three seconds and come back five
  seconds later.

## Saved data

Settings and progress are saved as two small files, `settings.json` and `progress.json`:

- Windows: `%APPDATA%\JumpScape\`
- macOS and Linux: `~/.config/JumpScape/`

Delete them to reset the game.

## Project structure

```text
Program.cs                Entry point
Source/
  JumpScapeGame.cs        Window, screen switching and drawing
  Core/                   Assets, input, settings, save files and screen scaling
  Gameplay/               The player, platforms, enemies, door, key, inventory and level loading
  Menus/                  Main menu, level picker, settings, and the pause and death screens
Content/
  Content.mgcb            List of every image, sound and font the game uses
  Fonts/ Graphics/ Sounds/
Levels/                   Level files (see Levels/README.md)
```

The images, sounds and font are built by the MonoGame content pipeline. If you add a new asset, add it to
`Content/Content.mgcb`, either by hand or with the MGCB Editor (`dotnet mgcb-editor Content/Content.mgcb`).

## License

This project is licensed under the MIT License.
