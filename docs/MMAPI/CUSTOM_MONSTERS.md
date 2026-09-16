# Custom Monsters

[← MMAPI](MMAPI.md)

A monster uses Fiddle prototype data, sprites, a GML object, and a state machine. The bundled example is under `examples/exampleenemyroller/`.

## Changing An Existing Monster

Patch an existing monster under its current category. This example changes the mushroom's health and idle sprites:

```toml
# fiddle/monsters/shroom.toml
[mushroom]
hp = 40

[mushroom.sprites.idle]
north = "spr_my_shroom_idle"
south = "spr_my_shroom_idle"
```

Include only the fields you want to change. Tables merge into the existing entry, while an array replaces that field's whole array. Add sprites through `images/`, or replace game sprites through `images/replace/`. Keep the monster key in its original category.

## Adding To An Existing Category

Add the monster table to the built-in category's Fiddle file. A new Shroom monster belongs in `fiddle/monsters/shroom.toml` and uses that category's defaults and sprite states. It may inherit `obj_monster_shroom`, or name a new `par_monster` child in `gm_object`.

## Adding A Monster

The Roller uses this layout:

```text
exampleenemyroller/
├─ manifest.toml
├─ momi/
│  └─ monster_categories/
│     └─ example_enemy_roller.toml
├─ fiddle/
│  └─ monsters/
│     └─ example_enemy_roller.toml
├─ gml/
│  └─ ExampleEnemyRoller.gml
├─ images/
│  ├─ spr_example_enemy_roller_idle.png
│  ├─ spr_example_enemy_roller_idle.meta.toml
│  └─ ...
└─ shapes/
   └─ Monsters/
      └─ ExampleEnemyRoller/
         └─ poly_example_enemy_roller_idle.meta.toml
```

Use a lower snake case name for the monster and its category. The example uses `example_enemy_roller`; its object is `obj_example_enemy_roller`.

## The Category

`momi/monster_categories/example_enemy_roller.toml` lists the sprite states in their numeric order:

```toml
states = ["windup", "walk", "attack", "hurt", "dying"]
```

Category files live directly under `momi/monster_categories/`. Each contains one `states` array of distinct, nonempty names.

The GML state enum begins in the same order:

```gml
enum ExampleEnemyRollerState {
    Windup,
    Walk,
    Attack,
    Hurt,
    Dying,
    Bounce,
    Stunned,
    LEN
}
```

The first five values match the sprite catalogue. `Bounce` and `Stunned` are behavior-only states.

Declare a category only when no built-in category fits. A custom category belongs to the mod that declares it.

## The Prototype

The prototype lives at `fiddle/monsters/<category>.toml`. Its table name is the monster's string id:

```toml
[example_enemy_roller]
hp = 40
damage = 20
essence = 0
iframes = 7
gm_object = "obj_example_enemy_roller"
hurtbox = "spr_example_enemy_roller_idle"
starting_dir = [0, 360]
damage_number_offset = -15
patience_acknowledgement_reset = -120
aggro_radius = 192
use_circle = false
status_effect_offset = -8
fire_effect_offset = -4
drops = []

[example_enemy_roller.sprites]
windup = "spr_example_enemy_roller_idle"
walk = "spr_example_enemy_roller_walk"
attack = "spr_example_enemy_roller_run_attack"
hurt = "spr_example_enemy_roller_idle"
dying = "spr_example_enemy_roller_idle"

[example_enemy_roller.tango]
```

The game applies `[default]`, when present, before the monster table. The merged prototype must contain:

- numeric `hp`, `damage`, `essence`, `iframes`, `damage_number_offset`, `patience_acknowledgement_reset`, `aggro_radius`, `status_effect_offset`, and `fire_effect_offset`;
- a two-number `starting_dir`;
- a `gm_object` beginning with `obj_`;
- a sprite named by `hurtbox`, and optionally another named by `hitbox`;
- one `sprites` entry for every category state;
- a `tango` table and a `drops` array.

`coin_count` defaults to `[0, 0]`, and `use_circle` defaults to `false`. A state sprite may be one sprite name or a table containing `north` and `south`, with an optional `east`.

## The Object

Create the object named by `gm_object` as a child of `par_monster`:

```gml
object_create("obj_example_enemy_roller", object_reserve("par_monster"), {
    sprite_index: undefined,
    create: function() {
        event_inherit(ObjectEvent.Create);
    },
});
```

The inherited Create event chooses the initial sprite and creates the combat receiver. Leave `sprite_index` undefined until it runs.

MOMI checks literal `object_create` calls and warns when construction is indirect. Monster keys and category names must be unique across the install. Two mods cannot create the same object, though monsters from one mod may share one.

## The State Machine

The Roller builds one `StateMachineBuilder` in Create. Each state keeps its setup, per-frame work, and cleanup in `start`, `step`, and `stop`. The object calls `fsm.step()` before movement and `fsm.end_step()` before consuming damage.

Use `fsm.state_frame` for timing. Put temporary resources in the state that owns them: the Roller's Attack state creates its damage volume in `start` and destroys it in `stop`.

See [`ExampleEnemyRoller.gml`](../../examples/exampleenemyroller/gml/ExampleEnemyRoller.gml) for the object implementation.

## Sprites And Collision

Each sprite needs a `.png` and matching `.meta.toml` under `images/`. A paired shape under `shapes/` supplies the monster and damage-receiver masks through `animation_to_shape(sprite_index)`.

The Roller's charge dimensions and offset are Fiddle fields used by its `TarballBuilder`, beside the rest of its tuning values.

## Saves

The game counts custom kills during the current session. MMAPI leaves them out of the game's saved per-species table because `MonsterId` numbers depend on the current install. Store persistent species history by string name in [MMAPI mod save data](API_REFERENCE.md#mod-save-files).

## Trying The Example

Install the example, load a save, and press F5 to spawn the Roller beside the player. Debug logging records its setup, state changes, collisions, and damage.
