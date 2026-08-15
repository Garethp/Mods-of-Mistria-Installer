# Engine Fix: save_load_date_history_tolerance

Lets a save whose date history remembers a since-removed custom NPC load anyway. The affected date memories are dropped with a logged warn instead of the load aborting.

`save_load_date_history_tolerance` is an **engine fix**, a hook-less edit. It dispatches nothing, and there is no handler to register. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Cycle/LoadGame.gml` |
| **Locator** | text anchor on the `date_history` deserialize |
| **Op** | text (filter loop) |
| **Marker** | `mmapi_save_date_history_tolerance` |

## The Edit

```gml
    ARI.date_history = []; // mmapi_save_date_history_tolerance
    for (var __mmapi_dhi = 0; __mmapi_dhi < array_length(files.player.date_history); __mmapi_dhi++) {
        var __mmapi_dh = files.player.date_history[__mmapi_dhi];
        if (try_string_to_npc_id(__mmapi_dh.npc) == undefined) {
            warn("MMAPI: save carried a date history entry for unknown NPC '{}' - dropped", __mmapi_dh.npc);
            continue;
        }
        __mmapi_dh.date = string_to_date(__mmapi_dh.date);
        __mmapi_dh.npc = string_to_npc_id(__mmapi_dh.npc);
        array_push(ARI.date_history, __mmapi_dh);
    }
```

## Why

Every date memory names its NPC, and the load resolves that name through fatal `string_to_npc_id` before any consumer runs. Date-history consumers themselves never resolve names, which is why a tombstoned NPC's memories stay safely inert, but the load line resolves eagerly. On an install with no tombstone for the name, one date memory aborts the load natively, with no dialog and nothing in any log.

The fix rebuilds the history without the unresolvable entries and names each drop in the log. A dropped entry no longer counts toward the shared date cooldown, which is the accepted amputation cost of running without the tombstone. Re-saving writes the cleansed history.

Zero-registrant inert: for names that resolve, the loop performs the same conversions on the same entries in the same order, so an intact install is behaviorally identical.
