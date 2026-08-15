# Engine Fix: vacant_roommate_load_guard

The load-time twin of [vacant_roommate_new_day_guard](vacant_roommate_new_day_guard.md): LoadGame wakes every roommate's brain after deserialize, and a tombstoned spouse's brain has nothing to do while it is parked offscreen.

`vacant_roommate_load_guard` is an **engine fix**, a hook-less edit. It dispatches nothing, and there is no handler to register. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Cycle/LoadGame.gml` |
| **Locator** | text anchor on the post-load roommate wake loop |
| **Op** | text (guarded predicate) |
| **Marker** | `mmapi_vacant_roommate_load_guard` |

## The Edit

```gml
        if (npc.is_roommate() && !mmapi_ext_is_vacant("npc_roster", i)) { // mmapi_vacant_roommate_load_guard
            npc.brain_dead = false;
        }
```

## Why

The [LoadGame blob guard](npc_load_missing_blob_guard.md) parks every vacancy in the offscreen pen with `brain_dead` set. This loop runs later in the same load and would wake any NPC whose persisted `<npc>_status` says roommate, vacancy included. Waking it changes nothing visible on its own (the park holds until the next day boundary), but the design intent is that a vacancy's brain never runs, and the new-day guard's containment should not depend on the load path leaving the flag alone.

Zero-registrant inert: the vacant table is empty on an install with no tombstones, so the condition equals the vanilla one.
