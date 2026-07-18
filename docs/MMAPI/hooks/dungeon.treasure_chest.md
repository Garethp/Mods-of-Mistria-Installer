# Hook: dungeon.treasure_chest

Know the moment a treasure chest starts its drop chain.

`dungeon.treasure_chest` is an **event** hook. Register a callback with `mmapi_on`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires in the dungeon breakables logic when a treasure chest starts its drop chain. ctx is `{ node, object_id, x, y }`, with `x` and `y` read from the node renderer. The emit lands right after the chest's timer-led drop chain is created, so the drops themselves have not resolved yet. To change what the chest yields, filter [items.treasure_distribution](items.treasure_distribution.md) instead.

| | |
| --- | --- |
| **Fires** | In the dungeon breakables logic, when a treasure chest starts its drop chain. |
| **ctx** | `{ node, object_id, x, y }` |
| **Kind contract** | The callback observes the moment. Its return value is ignored. |

### The ctx struct

- `node` - the chest's grid node.
- `object_id` - the chest's object id, `node.object_id`.
- `x`, `y` - the chest's world position, read from `node.renderer`.

## Usage

```gml
// dungeon.treasure_chest is an EVENT: the return value is ignored.
// You cannot change or stop it here; the return value is ignored.
function chest_counter_dungeon_treasure_chest(_ctx) {
    // _ctx is { node, object_id, x, y }.
    //   .node      - the chest's grid node.
    //   .object_id - node.object_id, which chest object this is.
    //   .x, .y     - the chest's world position (node.renderer.x/y).
    // your code here - e.g. tally chests opened this run
}

// inside your latched register function (see Mod Anatomy):
mmapi_on("dungeon.treasure_chest", chest_counter_dungeon_treasure_chest);
```

## Engine Wiring

- Seam [`dungeon_treasure_chest`](../seams/dungeon_treasure_chest.md) dispatches from `gml/scripts/GameplaySystems/Data/Grid/Breakables.gml`, right after the chest's drop chain is created.

## See Also

- [items.treasure_distribution](items.treasure_distribution.md) - Filter the dungeon treasure roll itself.
- [dungeon.side_room_chance](dungeon.side_room_chance.md) - Adjust the odds of treasure side rooms.
- [dungeon.floor_built](dungeon.floor_built.md) - The floor's room is fully built.
