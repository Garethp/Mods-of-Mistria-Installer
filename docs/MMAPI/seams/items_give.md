# Seam: items_give

Routes every item grant through a struct filter at the head of the engine's one give-item entry point.

`items_give` is a **text seam** (filter-shaped: it dispatches `mmapi_apply_filters`). It feeds [items.give](../hooks/items.give.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Player/Ari.gml` |
| **Locator** | text anchor at the head of `give_item()`, right after the LiveItem coercion |
| **Op** | text (filter dispatch, struct-valued) |
| **Feeds** | [`items.give`](../hooks/items.give.md) |
| **Value filtered** | the struct `{ item, item_id, count, show_toast, show_new_popup, play_sound }` |
| **ctx built** | `self` (the player) |
| **Marker** | `mmapi_items_run_give_filters` |

## The Edit

The injected block lands right after `give_item(item, count=1, show_toast=true, show_new_popup=true, play_sound=true)` coerces its argument (`item = is_struct(item) ? item : new LiveItem(item);`), so the struct always carries a `LiveItem`. It builds a six-field value struct from the (coerced) arguments:

```gml
var __mmapi_items_give_ctx = {
    item: item,
    item_id: item.item_id,
    count: count,
    show_toast: show_toast,
    show_new_popup: show_new_popup,
    play_sound: play_sound,
};
```

then dispatches `mmapi_apply_filters("items.give", __mmapi_items_give_ctx, self)` inside a try/catch. If the filtered struct comes back non-`undefined`, the seam re-reads each field defensively (`item`, `count`, `show_toast`, `show_new_popup`, `play_sound`, each in its own try/catch) and writes it back into the function's locals. That per-field isolation means an `undefined` return, a partial replacement struct, or a struct missing fields keeps the engine values for whatever is absent instead of crashing `give_item`.

`item_id` is the one field never read back: it is informational, a convenience for handlers matching on the item's id. Changing it does nothing. Replace `item` itself to substitute the grant.

## See Also

- [items.give](../hooks/items.give.md) - This is the hook this seam dispatches.
- [items_dropped](items_dropped.md) - This is the emit for items leaving the inventory into the world.
- [items_use_guard](items_use_guard.md) - This is the guard on using an item once held.
