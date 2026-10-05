# Engine Fix: save_load_used_objects_tolerance

Lets a save whose daily used-objects flags name a since-removed object load anyway. Unknown names are skipped, and [save_load_forget_warn](save_load_forget_warn.md) names them in the log.

`save_load_used_objects_tolerance` is an **engine fix**, an anchored edit with no hook behind it. Nothing dispatches. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Cycle/LoadGame.gml` |
| **Locator** | text anchor on the `used_object_today` deserialize |
| **Feeds** | (no hook) |
| **Marker** | `mmapi_save_used_objects_tolerance` |

## The Edit

The replace swaps the converter the array loader calls for its tolerant variant.

```gml
    ARI.used_object_today = files.player["used_object_today"] != undefined
        ? deserialize_array_bool(files.player.used_object_today, function(__mmapi_s) { return try_string_to_object_id(__mmapi_s); }, ObjectId.LEN) // mmapi_save_used_objects_tolerance
        : array_bool(ObjectId.LEN);
```

The daily used-objects flags are stored as a list of object names and resolved with fatal `string_to_object_id`, while the perks, items, and recipes lines around it use the tolerant `try_` variants. Object prototypes are fiddle content, so mods can mint object names, and one stale name aborts the load natively.

The fix is the same swap as the spells line. Unknown names route through the skip `deserialize_array_bool` already has, and [save_load_forget_warn](save_load_forget_warn.md) names them. A skipped flag means the object counts as unused today, which is the mildest possible loss.

For names that resolve, the `try_` variant returns the same ordinal as the fatal one, so an intact install behaves exactly as before.

## See Also

- [save_load_forget_warn](save_load_forget_warn.md) - Names the skipped object in the log.
- [save_load_spells_tolerance](save_load_spells_tolerance.md) - The same swap, for learned spells.
