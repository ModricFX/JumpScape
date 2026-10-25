# TODO

## Gameplay

- [ ] A boss level every 10 levels.

## Look and sound

- [ ] Running animation for the player (needs new sprite frames).
- [ ] Separate sounds for throwing an orb, defeating an enemy, picking up a key or a gem, collecting a
      power-up, touching a checkpoint and bouncing. They reuse the jump and hit sounds now.
- [ ] Different music in the nether and the night sky.
- [ ] Proper art for spikes, gems, checkpoint flags and bounce pads. They are drawn from simple shapes now.

## Controls and settings

- [ ] Choosing your own gamepad buttons, like the keys.

## Making levels

- [ ] More in the level editor: deleting and renumbering levels, any sky and tint color (only a few
      presets now), and copying things from one area to another.
- [ ] A **Check** button in the level editor that lets the automated player try the level and shows where
      it gets stuck. The automated player lives in Tests now, so it would move into the game.
- [ ] Run the checks in Tests automatically on GitHub after every change.
- [ ] Build releases for Windows, macOS and Linux automatically with GitHub Actions.
