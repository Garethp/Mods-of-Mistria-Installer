# Engine Fix: save_load_pet_items_tolerance

Unknown items queued on the pet are dropped with a warn instead of aborting the load.

`save_load_pet_items_tolerance` is an **engine fix**, an anchored edit with no hook behind it. Nothing dispatches. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/Pet.gml` |
| **Locator** | text anchor on the `items_to_pop` deserialize |
| **Feeds** | (no hook) |
| **Marker** | `mmapi_save_pet_items_tolerance` |

## The Edit

The replace filters the queue through the tolerant variant. It resets `self.items_to_pop` to an empty array, with the marker as a trailing comment, then walks `pet_data.items_to_pop` by index, resolves each name with `try_string_to_item_id`, logs a warn and skips an unknown name, and pushes each resolved id.

The pet's pending item drops are stored by name and resolved with fatal `string_to_item_id`, while the item flag arrays elsewhere in the load use the tolerant `try_` variants. A custom item sitting in the pet's queue when its mod is removed aborts the load natively.

The fix filters the queue through the tolerant variant with a warn per dropped item. The pet delivers the rest of its queue as normal.

Resolvable items produce the same queue in the same order as the vanilla `array_map`, so an intact install is unchanged.

## See Also

- [save_load_renown_item_tolerance](save_load_renown_item_tolerance.md) - Another pending item name resolved at load.
- [save_load_infusion_tolerance](save_load_infusion_tolerance.md) - The item-side lookup every inventory passes through.
