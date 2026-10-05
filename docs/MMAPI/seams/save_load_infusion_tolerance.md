# Engine Fix: save_load_infusion_tolerance

Lets a save load when an item carries a since-removed custom infusion. The infusion is dropped from the item with a logged warn instead of the load aborting.

`save_load_infusion_tolerance` is an **engine fix**, an anchored edit with no hook behind it. Nothing dispatches. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Items/LiveItem.gml` |
| **Locator** | text anchor on the infusion branch of `deserialize_live_item` |
| **Feeds** | (no hook) |
| **Marker** | `mmapi_save_infusion_tolerance` |

## The Edit

The replace resolves the infusion through the tolerant variant.

```gml
    if !is_nullish(item.infusion) {
        var __mmapi_inf = try_string_to_infusion(item.infusion); // mmapi_save_infusion_tolerance
        if (__mmapi_inf == undefined) {
            warn("MMAPI: save carried an item with unknown infusion '{}' - dropped", item.infusion);
        } else {
            live_item.infusion = __mmapi_inf;
        }
    }
```

Every serialized item records its infusion by name, and `deserialize_live_item` resolves it with fatal `string_to_infusion`. Infusions load from fiddle content, so mods can mint infusion names, and one infused tool in any inventory, chest, or lost-and-found aborts the load natively. The engine already uses the tolerant variant at its other infusion read sites, so this line is the exception.

The fix resolves through the tolerant variant and, when the name is unknown, leaves the item's infusion untouched rather than assigning `undefined`, because the `LiveItem` constructor seeds infusion from the prototype's `default_infusion` and overwriting it would strip a legitimate default. The item survives uninfused.

A resolvable infusion assigns the same value as the fatal path, so an intact install is unchanged.

## See Also

- [save_load_renown_item_tolerance](save_load_renown_item_tolerance.md) - Another fatal item-side lookup the family covers.
- [save_load_pet_items_tolerance](save_load_pet_items_tolerance.md) - The pet's item queue, filtered the same way.
