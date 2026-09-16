# Example Enemy Roller

This package shows a custom monster with its own prototype, sprites, object, collision shape, and state machine. See [Custom Monsters](../../docs/MMAPI/CUSTOM_MONSTERS.md) for the file format.

## Package Contents

- `momi/monster_categories/example_enemy_roller.toml` defines the sprite-state order.
- `fiddle/monsters/example_enemy_roller.toml` contains the prototype and tuning.
- `gml/ExampleEnemyRoller.gml` creates the `par_monster` child and its behavior.
- `images/` and `shapes/` contain the art and collision shape.

The monster wanders until the player approaches, pauses, charges, bounces from the player or a wall, and lands stunned. It uses the standard combat receiver for sword damage, hit effects, status effects, death, and loot.

## Trying It

Install the package, load a save, and press F5. One Roller spawns beside the player.

Movement, charge size, bounce, stun duration, health, damage, and drops are set in the Fiddle prototype. Debug logging records the Roller's setup and combat states while the game runs.

The base drop table contains one copper ore and 40 coins. Game perks may change the final payout.
