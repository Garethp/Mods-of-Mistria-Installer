# Hook: player.skill_leveled

Know the moment the player levels up a skill.

`player.skill_leveled` is an **event** hook. Register a callback with `mmapi_on`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires in `gain_xp()` when the applied XP raises the skill's level, right after the new total is written and before the level-up celebration. ctx is `{ skill, old_level, new_level, silent }`.

`silent` is `gain_xp`'s own flag, and the emit sits outside its gate, so silent engine gains (crafting XP, stable breeding) fire too, celebration or not. A level lowered through a negative [player.xp_delta](player.xp_delta.md) replacement never fires this hook. A single large gain can cross several levels and still fires once, so compare `old_level` and `new_level` rather than assuming a step of one. Save load and the debug menu write `skill_xp` directly and never fire.

| | |
| --- | --- |
| **Fires** | In `gain_xp()`, right after the XP write raises the skill's level, before the celebration. |
| **ctx** | `{ skill, old_level, new_level, silent }` |
| **Kind contract** | The callback observes the moment. Its return value is ignored. |

### The ctx struct

- `skill` - the `Skill` enum id that leveled.
- `old_level` - the level before the XP applied.
- `new_level` - the level after (a big gain can cross more than one).
- `silent` - `gain_xp`'s celebration-skip flag. The event fires either way.

## Usage

```gml
// player.skill_leveled is an EVENT: the return value is ignored.
// You cannot change or stop it here; the return value is ignored.
function milestone_bells_player_skill_leveled(_ctx) {
    // _ctx is { skill, old_level, new_level, silent }.
    //   .skill     - the Skill enum id that leveled.
    //   .old_level - the level before the gain.
    //   .new_level - the level after (may have crossed several).
    //   .silent    - true for celebration-skipping gains (crafting, breeding).
    // Fires once per leveling gain, never on plain XP ticks.
    // if (_ctx.skill == Skill.Cooking && _ctx.new_level >= 10) { ... }
}

// inside your latched register function (see Mod Anatomy):
mmapi_on("player.skill_leveled", milestone_bells_player_skill_leveled);
```

## Engine Wiring

- Seam [`player_xp_delta`](../seams/player_xp_delta.md) dispatches from `gml/scripts/GameplaySystems/Player/Ari.gml`, inside `gain_xp()` right after the capped XP write. The same edit filters [player.xp_delta](player.xp_delta.md) at the function's head.

## See Also

- [player.xp_delta](player.xp_delta.md) - Change every skill XP gain before it applies, the filter that decides whether this event fires.
- [renown.level_gained](renown.level_gained.md) - This event is the renown counterpart of this moment.
- [player.acquire_perk](player.acquire_perk.md) - Know when the player acquires a perk.
