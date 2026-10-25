# TODO

## Ideas

- [ ] Running animation for the player (needs new sprite frames).
- [ ] A boss level every 10 levels.
- [ ] Choosing your own keys in the settings.

## Done

- [x] Ten levels from easy to hard, each with a name and a description. An automated player finishes every
      one of them, so they are known to be possible.
- [x] More settings: show timer, extra hearts (easier), unlock all levels, VSync, a controls list and a
      reset button. The settings work with the keyboard too.
- [x] Better level picker: level names and descriptions, locked levels, the next level glows, arrow keys
      move around the grid, and a count of finished levels.
- [x] Pause menu: Restart level and Controls.
- [x] Levels are JSON files with a schema (autocomplete and error checking in VS Code) and clear in-game
      error messages, instead of a home-made text format.
- [x] NPCs you can talk to: Pip explains the controls in Level 1 and gives tips in later levels.
- [x] Race level: a ghost of your best run races you, with a timer and best times in the level picker.
- [x] Better looking game and menus in the same style: wooden pause, game over and level complete screens,
      blurred forest backdrop, platform shadows, dust, level banners, key hints, stars for finished levels.
- [x] Level complete screen with the time taken and Next level / Play again / Main menu.
- [x] Music streams from a small file instead of an 18 MB sound held in memory.
- [x] More than one door per level. Each locked door uses one key (tested with two doors and one key,
      and with two doors and two keys).
- [x] Areas and portals: a nether region and a night sky behind portal doors.
- [x] Weapons: click to throw a glowing orb towards the mouse. One orb defeats a frog or a ghost.
- [x] Power-ups: heart (+1 heart), double jump and high jump (15 s each).
- [x] Fade out when the player clicks Retry on the death screen.
- [x] Press Q to drop the selected item (select it with 1, 2 or 3 first).
- [x] Picked-up items disappear from the level.
- [x] Unlocking a door with two keys in the inventory only uses one.
- [x] The half heart shows the empty half.
- [x] Levels work on every screen size.
- [x] Moving left while holding right (and the other way round) now switches direction.
