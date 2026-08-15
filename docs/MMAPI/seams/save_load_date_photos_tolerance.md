# Engine Fix: save_load_date_photos_tolerance

Lets a save whose date photos picture a since-removed custom NPC load anyway. The affected photos are dropped with a logged warn instead of the load aborting.

`save_load_date_photos_tolerance` is an **engine fix**, a hook-less edit. It dispatches nothing, and there is no handler to register. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Cycle/LoadGame.gml` |
| **Locator** | text anchor on the `DATE_PHOTOS` deserialize |
| **Op** | text (filter loop) |
| **Marker** | `mmapi_save_date_photos_tolerance` |

## The Edit

```gml
    DATE_PHOTOS = []; // mmapi_save_date_photos_tolerance
    for (var __mmapi_dpi = 0; __mmapi_dpi < array_length(files.date_photos.photos); __mmapi_dpi++) {
        var __mmapi_dp = files.date_photos.photos[__mmapi_dpi];
        if (try_string_to_npc_id(__mmapi_dp.npc) == undefined) {
            warn("MMAPI: save carried a date photo of unknown NPC '{}' - dropped", __mmapi_dp.npc);
            continue;
        }
        array_push(DATE_PHOTOS, {
            photo: __mmapi_dp.photo,
            photo_decompressed: undefined,
            timestamp: __mmapi_dp.timestamp,
            npc: string_to_npc_id(__mmapi_dp.npc),
            date: string_to_date(__mmapi_dp.date),
        });
    }
```

## Why

Every date photo names its NPC, and the load resolves the name through fatal `string_to_npc_id`. This was the first NPC-domain brick vector the extension-points probes identified, and the tombstone is its primary protection. Without a tombstone, one photo aborts the load natively.

The fix drops the affected photos with a warn each and keeps the rest. The photo blob is itself the memory of the mod-era date, so there is nothing meaningful to preserve once the NPC cannot be named. Re-saving writes the cleansed album.

Zero-registrant inert: resolvable photos are rebuilt with the same fields and conversions in the same order, so an intact install is behaviorally identical.
