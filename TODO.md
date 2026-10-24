# TODO

## Needed

- [ ] Weapons: attack in the direction of the mouse by clicking.
- [ ] Power-ups to pick up: extra health, double jump, higher jump.
      (The unused `ball_green` / `ball_red` images were removed during cleanup. Get them back from git
      history if they were meant for this.)

## Later

- [ ] Support more than one door per level, then test:
  - two doors and one key: only one door gets unlocked
  - two doors and two keys: each unlock uses one key, and both doors can be opened

## Ideas

- [ ] An NPC the player can talk to. A tutorial level could use it to explain the controls.
- [ ] Race level: a side-by-side competition, for example a boss level every 10 levels.
- [ ] Running animation for the player (needs new sprite frames).

## Done

- [x] Fade out when the player clicks Retry on the death screen.
- [x] Press Q to drop the selected item (select it with 1, 2 or 3 first).
- [x] Picked-up items disappear from the level.
- [x] Unlocking a door with two keys in the inventory only uses one.
- [x] The half heart shows the empty half.
- [x] Levels work on every screen size.
- [x] Moving left while holding right (and the other way round) now switches direction.
