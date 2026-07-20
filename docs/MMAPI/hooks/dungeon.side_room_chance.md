# Hook: dungeon.side_room_chance

Adjust the odds of dungeon side rooms.

`dungeon.side_room_chance` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Filters the spawn chance passed to `try_create_side_room()` at the head of that function, before the per-floor roll. ctx is `{ impl, is_ritual, max_flr }`. `is_ritual` is `true` when `impl == DungeonImpl.Ritual`, the pre-computed convenience flag, so consumers need not reference the `DungeonImpl` enum. Return an adjusted chance (0-100) to raise or lower the odds of a treasure/ritual side room. Return the value unchanged (or `undefined`) to defer. Fires for every side-room impl the runner attempts.

| | |
| --- | --- |
| **Fires** | At the head of `try_create_side_room()`, before the per-floor roll. |
| **Value** | The side-room spawn chance, 0-100. |
| **ctx** | `{ impl, is_ritual, max_flr }` |
| **Kind contract** | The callback receives the current value and returns a replacement, or `undefined` to keep the current value. |

### The ctx struct

- `impl` - the side-room `DungeonImpl` the runner is attempting.
- `is_ritual` - `true` when `impl == DungeonImpl.Ritual`, pre-computed so your handler never has to reference the `DungeonImpl` enum.
- `max_flr` - the `max_flr` argument `try_create_side_room()` received for this impl.

## Usage

```gml
// dungeon.side_room_chance is a FILTER: you receive (value, ctx) and return a
// replacement, or undefined to keep the game's value.
function lucky_delver_dungeon_side_room_chance(_value, _ctx) {
    // _value is the side-room spawn chance, 0-100, before the per-floor roll.
    // _ctx is { impl, is_ritual, max_flr }.
    //   .impl      - the side-room DungeonImpl being attempted.
    //   .is_ritual - true when impl == DungeonImpl.Ritual (pre-computed, no
    //                enum reference needed).
    //   .max_flr   - the max_flr argument try_create_side_room() received.
    if (_value == undefined) return undefined; // test undefined BEFORE anything else
    // double treasure-room odds, leave ritual rooms alone:
    if (!_ctx.is_ritual) return min(100, _value * 2);
    return undefined; // undefined = keep the game's value
}

mmapi_filter("dungeon.side_room_chance", lucky_delver_dungeon_side_room_chance);
```

## Engine Wiring

- Seam [`dungeon_side_room_chance`](../seams/dungeon_side_room_chance.md) dispatches from `gml/scripts/GameplaySystems/Dungeon/DungeonRunner.gml`, at the head of `try_create_side_room()`.

## See Also

- [dungeon.treasure_chest](dungeon.treasure_chest.md) - A treasure chest starts its drop chain.
- [items.treasure_distribution](items.treasure_distribution.md) - Filter the dungeon treasure roll itself.
- [dungeon.floor_enter](dungeon.floor_enter.md) - This hook fires as each floor is entered.
