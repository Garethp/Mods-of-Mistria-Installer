# Seam: items_use_guard

Puts a veto check in front of every item use, right after the LiveItem coercion.

`items_use_guard` is a **template seam** (`op = "guard"`). It feeds [items.use_guard](../hooks/items.use_guard.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Items/use_item.gml` |
| **Locator** | structural target: `use_item`, at after `if !is_struct(item) { item = new LiveItem(item); }` |
| **Op** | `guard` |
| **Feeds** | [`items.use_guard`](../hooks/items.use_guard.md) |
| **ctx built** | `item` (the `LiveItem` being used) |
| **On veto** | `return false;` |
| **Marker** | `mmapi_items_run_use_guards` |

## The Edit

The structural target places the generated dispatch immediately *after* the coercion line `if !is_struct(item) { item = new LiveItem(item); }` inside `use_item()` (deliberately, so ctx is always a `LiveItem` whether the caller passed a struct or a bare item id) and before any use logic runs. The dispatch calls `mmapi_check_guards("items.use_guard", item)` in the uniform try/catch shape (catch var `__mmapi_item_use`). When any guard returns `false`, the injected line runs `return false;`, so `use_item` reports the use as not having happened. Every other return allows.

The locator is matched token-wise inside `use_item`, immune to whitespace and comment drift. With zero handlers the guard check early-outs on an empty registry, leaving pristine behavior.

## See Also

- [items.use_guard](../hooks/items.use_guard.md) - This is the hook this seam dispatches.
- [items_consumed](items_consumed.md) - This is the event emit after the player eats an item.
- [items_give](items_give.md) - This is the struct filter on every item grant.
