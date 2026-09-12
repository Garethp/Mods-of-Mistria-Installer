# Hook: items.chest_opened

Know the moment any chest item finishes opening.

`items.chest_opened` is an **event** hook. Register a callback with `mmapi_on`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires in the hold-to-use completion's `OpenChest` case, after the chest's loot table is resolved and before the loot roll and gold payout. ctx is `{ live_item, table }`. It fires for vanilla and modded chests alike, so a handler that only cares about its own chests must check the item first.

| | |
| --- | --- |
| **Fires** | When a chest item finishes its open hold, loot table resolved, drops still pending. |
| **ctx** | `{ live_item, table }` |
| **Kind contract** | The callback observes the moment. Its return value is ignored. |

### The ctx struct

- `live_item` - the chest `LiveItem` being opened.
- `table` - the resolved `FISH_CHEST` loot table, `{ gold, items }`. This is the engine's shared table struct, so treat it as read-only.

## Usage

```gml
// items.chest_opened is an EVENT: the return value is ignored.
// You cannot change or stop it here; the return value is ignored.
function bonus_drop_items_chest_opened(_ctx) {
    // _ctx is { live_item, table }.
    //   .live_item - the chest LiveItem being opened.
    //   .table     - the resolved FISH_CHEST table (read-only).
    // Fires for every chest, so gate on your own items first.
    if (_ctx.live_item.prototype.tags.contains("my_mod_chest")) {
        var _id = try_string_to_item_id("my_mod_bonus_item");
        if (_id != undefined) { drop_item(new LiveItem(_id), obj_ari.x, obj_ari.y); }
    }
}

// inside your latched register function (see Mod Anatomy):
mmapi_on("items.chest_opened", bonus_drop_items_chest_opened);
```

## Interactions

- [items.use_guard](items.use_guard.md) fires first and can veto the whole use, in which case this event never dispatches.
- The chest's loot and gold land through `drop_item` after this event, so [items.dropped](items.dropped.md) fires once per drop that follows it.

## Engine Wiring

- Seam [`fish_chest_table_lookup`](../seams/fish_chest_table_lookup.md) dispatches from `gml/scripts/Player/AriFsm.gml`, immediately after the `OpenChest` table switch. The same seam resolves the table for items carrying the [`fish_chest`](../TREASURE_CHESTS.md) data field.

## See Also

- [Treasure Chests](../TREASURE_CHESTS.md) - Declaring custom chests and loot tables in fiddle data.
- [items.dropped](items.dropped.md) - Know what is about to drop into the world.
- [items.consumed](items.consumed.md) - Another item use event. It fires after the player eats an item.
