# Engine Fix: save_load_used_objects_tolerance

Lets a save whose daily used-objects flags name a since-removed object load anyway. Unknown names are skipped, and [save_load_forget_warn](save_load_forget_warn.md) names them in the log.

`save_load_used_objects_tolerance` is an **engine fix**, a hook-less edit. It dispatches nothing, and there is no handler to register. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Cycle/LoadGame.gml` |
| **Locator** | text anchor on the `used_object_today` deserialize |
| **Op** | text (functor swap) |
| **Marker** | `mmapi_save_used_objects_tolerance` |

## The Edit

```gml
    ARI.used_object_today = files.player["used_object_today"] != undefined
        ? deserialize_array_bool(files.player.used_object_today, try_string_to_object_id, ObjectId.LEN) // mmapi_save_used_objects_tolerance
        : array_bool(ObjectId.LEN);
```

## Why

The daily used-objects flags are stored as a list of object names and resolved with fatal `string_to_object_id`, while the perks, items, and recipes lines around it use the tolerant `try_` variants. Object prototypes are fiddle content, so mods can mint object names, and one stale name aborts the load natively.

The fix is the same functor swap the spells line received: unknown names route through `deserialize_array_bool`'s existing skip and the forget-warn fix names them. A skipped flag means the object counts as unused today, which is the mildest possible amputation.

Zero-registrant inert: for names that resolve, the `try_` variant returns the same ordinal as the fatal one, so an intact install is behaviorally identical.
