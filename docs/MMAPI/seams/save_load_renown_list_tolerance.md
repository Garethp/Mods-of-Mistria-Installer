# Engine Fix: save_load_renown_list_tolerance

Caller half of the renown tolerance pair. Keeps dropped entries out of the pending renown list so the end-of-day processor never sees them.

`save_load_renown_list_tolerance` is an **engine fix**, an anchored edit with no hook behind it. Nothing dispatches. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Cycle/LoadGame.gml` |
| **Locator** | text anchor on the `pending_renown_entries` deserialize |
| **Feeds** | (no hook) |
| **Marker** | `mmapi_save_renown_list_tolerance` |

## The Edit

The replace rebuilds the list, keeping only the entries that parsed.

```gml
    ARI.pending_renown_entries = List(); // mmapi_save_renown_list_tolerance
    for (var __mmapi_ri = 0; __mmapi_ri < array_length(files.player.pending_renown_entries); __mmapi_ri++) {
        var __mmapi_re = deserialize_renown_entry(files.player.pending_renown_entries[__mmapi_ri]);
        if (__mmapi_re != undefined) {
            ARI.pending_renown_entries.push(__mmapi_re);
        }
    }
```

With [save_load_renown_item_tolerance](save_load_renown_item_tolerance.md) in place, `deserialize_renown_entry` returns `undefined` for a pending museum donation of an unknown item. Vanilla's `map` would keep that `undefined` in the list, and the end-of-day renown processor dereferences every entry it walks, which would trade the load crash for an overnight crash. This rebuild keeps only real entries.

When every entry parses, the loop builds the same list in the same order as the vanilla `map`, so an intact install is unchanged.

## See Also

- [save_load_renown_item_tolerance](save_load_renown_item_tolerance.md) - The parser half of the pair.
- [save_load_pet_items_tolerance](save_load_pet_items_tolerance.md) - Another by-name item queue the load filters.
