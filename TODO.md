# TODO

## Gameplay

- [ ] Coyote time and jump buffering: a jump still works for a few frames after walking off an edge, and a
      jump pressed just before landing happens on landing. The later levels have tight jumps, so this would
      make them feel fairer.
- [ ] Short hops: letting go of jump early ends the jump lower, for more control.
- [ ] Checkpoint flags in long levels like Sky Tower and The Gauntlet, so dying doesn't always mean starting
      the climb again.
- [ ] Moving platforms (side to side or up and down) and bounce pads, as new things levels can use.
- [ ] Spikes or other hazards that hurt when touched.
- [ ] Hidden gems in each level, with how many you found shown in the level picker.
- [ ] A boss level every 10 levels.

## Look and sound

- [ ] Running animation for the player (needs new sprite frames).
- [ ] Separate sounds for throwing an orb, defeating an enemy, picking up a key and collecting a power-up.
      They reuse the jump and hit sounds now.
- [ ] Different music in the nether and the night sky.

## Progress and replay

- [ ] Bronze, silver and gold target times for each level, shown in the level picker.
- [ ] A total time for the whole game (the sum of the best times), to race against.

## Controls and settings

- [ ] Gamepad support.
- [ ] Choosing your own keys in the settings.

## Making levels

- [ ] A level editor in the game: place platforms, enemies and doors with the mouse and save the JSON file.
- [ ] Add the automated player that checked the levels to the repository as a test project, so every level
      is checked again after each change.
- [ ] Build releases for Windows, macOS and Linux automatically with GitHub Actions.
