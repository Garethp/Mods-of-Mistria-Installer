# Hook: dungeon.side_room_range

Place dungeon side rooms nearer to or deeper than the entry floor.

`dungeon.side_room_range` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Filters the floor span passed to `try_create_side_room()` at the head of that function, before the uniform floor pick. ctx is `{ impl, is_ritual, start_floor }`. `is_ritual` is `true` when `impl == DungeonImpl.Ritual`, the pre-computed convenience flag, so consumers need not reference the `DungeonImpl` enum. The side room's floor is drawn uniformly from `start_floor` through `start_floor` plus the value. Return a smaller span to keep the room near the entry floor, or a larger one to spread it deeper. A pick at floor 100 or past it yields no side room, and neither does a picked floor without an eligible room for the impl. Return the value unchanged (or `undefined`) to defer.

The engine gates each attempt before this hook fires. A treasure room needs the Treasure Hunter perk, a ritual chamber needs the Lost to History perk, and each is attempted at most once per day. When a gate holds the attempt back, `try_create_side_room()` is never called and nothing dispatches. When the gates pass, the hook fires once per side room impl the runner attempts.

| | |
| --- | --- |
| **Fires** | At the head of `try_create_side_room()`, before the uniform floor pick. |
| **Value** | The floor span for the pick. The side room's floor is drawn from `start_floor` through `start_floor` plus the value. |
| **ctx** | `{ impl, is_ritual, start_floor }` |
| **Kind contract** | The callback receives the current value and returns a replacement, or `undefined` to keep the current value. |

### The ctx struct

- `impl` - the `DungeonImpl` of the side room the runner is attempting.
- `is_ritual` - `true` when `impl == DungeonImpl.Ritual`, pre-computed so your handler never has to reference the `DungeonImpl` enum.
- `start_floor` - the floor the run enters on, the low end of the pick.

## Usage

```gml
// dungeon.side_room_range is a FILTER: you receive (value, ctx) and return a
// replacement, or undefined to keep the game's value.
function shallow_delver_dungeon_side_room_range(_value, _ctx) {
    // _value is the floor span for the side-room pick: the room's floor is
    // drawn from start_floor through start_floor + _value.
    // _ctx is { impl, is_ritual, start_floor }.
    //   .impl        - the side-room DungeonImpl being attempted.
    //   .is_ritual   - true when impl == DungeonImpl.Ritual (pre-computed, no
    //                  enum reference needed).
    //   .start_floor - the floor the run enters on, the low end of the pick.
    if (_value == undefined) return undefined; // test undefined BEFORE anything else
    // keep treasure rooms within three floors of the entrance, leave ritual
    // chambers alone:
    if (!_ctx.is_ritual) return min(_value, 3);
    return undefined; // undefined = keep the game's value
}

mmapi_filter("dungeon.side_room_range", shallow_delver_dungeon_side_room_range);
```

## Engine Wiring

- Seam [`dungeon_side_room_range`](../seams/dungeon_side_room_range.md) dispatches from `gml/scripts/GameplaySystems/Dungeon/DungeonRunner.gml`, at the head of `try_create_side_room()`.

## See Also

- [dungeon.treasure_chest](dungeon.treasure_chest.md) - A treasure chest starts its drop chain.
- [items.treasure_distribution](items.treasure_distribution.md) - Filter the dungeon treasure roll itself.
- [dungeon.floor_enter](dungeon.floor_enter.md) - This hook fires as each floor is entered.
