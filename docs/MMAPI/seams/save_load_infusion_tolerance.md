# Engine Fix: save_load_infusion_tolerance

Lets a save load when an item carries a since-removed custom infusion. The infusion is dropped from the item with a logged warn instead of the load aborting.

`save_load_infusion_tolerance` is an **engine fix**, a hook-less edit. It dispatches nothing, and there is no handler to register. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Items/LiveItem.gml` |
| **Locator** | text anchor on the infusion branch of `deserialize_live_item` |
| **Op** | text (guarded resolve) |
| **Marker** | `mmapi_save_infusion_tolerance` |

## The Edit

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

## Why

Every serialized item records its infusion by name, and `deserialize_live_item` resolves it with fatal `string_to_infusion`. Infusions load from fiddle content, so mods can mint infusion names, and one infused tool in any inventory, chest, or lost-and-found aborts the load natively. The engine already uses the tolerant variant at its two other infusion read sites, so this line is the inconsistency.

The fix resolves through the tolerant variant and, when the name is unknown, leaves the item's infusion untouched rather than assigning `undefined`, because the `LiveItem` constructor seeds infusion from the prototype's `default_infusion` and overwriting it would strip a legitimate default. The item survives uninfused.

Zero-registrant inert: a resolvable infusion assigns the same value as the fatal path.
