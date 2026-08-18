# Hook: renown.level_gained

Know the moment the player gains a renown level.

`renown.level_gained` is an **event** hook. Register a callback with `mmapi_on`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires inside `Ari.set_renown()` when the write raises the renown level, past the gains-only early return and before the crossed levels' rewards are granted. ctx is `{ old_level, new_level }`.

A single write can cross several levels and still fires once, so compare the two ctx fields. The engine's gameplay path here is the day-rollover drain of pending renown entries, each already filtered through [player.renown_delta](player.renown_delta.md), and debug sets route through `set_renown` too. Save load restores the renown field directly and never fires. Losses and level-neutral writes never fire either. `set_renown` bails before the emit unless the level rose.

| | |
| --- | --- |
| **Fires** | Inside `Ari.set_renown()`, when the write raises the level, before the level rewards are granted. |
| **ctx** | `{ old_level, new_level }` |
| **Kind contract** | The callback observes the moment. Its return value is ignored. |

### The ctx struct

- `old_level` - the renown level before the write.
- `new_level` - the level after (a big gain can cross more than one).

## Usage

```gml
// renown.level_gained is an EVENT: the return value is ignored.
// You cannot change or stop it here; the return value is ignored.
function town_herald_renown_level_gained(_ctx) {
    // _ctx is { old_level, new_level }.
    //   .old_level - the renown level before the write.
    //   .new_level - the level after (may have crossed several).
    // Fires at day rollover as the pending entries drain, once per
    // leveling write. The level rewards land right after the emit.
    // if (_ctx.new_level >= 50) { ... }
}

// inside your latched register function (see Mod Anatomy):
mmapi_on("renown.level_gained", town_herald_renown_level_gained);
```

## Engine Wiring

- Seam [`renown_gains`](../seams/renown_gains.md) dispatches from `gml/scripts/GameplaySystems/Player/Ari.gml`, inside `set_renown()` below its gains-only early return. The same seam emits [renown.rank_gained](renown.rank_gained.md) when the gain crosses a rank boundary.

## See Also

- [renown.rank_gained](renown.rank_gained.md) - Know when a gain crosses a rank boundary, from the same seam.
- [player.renown_delta](player.renown_delta.md) - Change every renown gain before it applies, upstream of this event.
- [player.skill_leveled](player.skill_leveled.md) - This event is the skill counterpart of this moment.
