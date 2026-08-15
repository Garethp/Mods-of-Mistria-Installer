# Engine Fix: vacant_roommate_new_day_guard

Keeps a tombstoned spouse or roommate parked offscreen instead of letting the new-day roommate branch teleport it into the player's home. The marriage itself is untouched, and reinstalling the mod resumes it.

`vacant_roommate_new_day_guard` is an **engine fix**, a hook-less edit. It dispatches nothing, and there is no handler to register. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/NPCs/NpcDatabase.gml` |
| **Locator** | text anchor on the new-day schedule-selection branch |
| **Op** | text (guarded predicate) |
| **Marker** | `mmapi_vacant_roommate_guard` |

## The Edit

The roommate predicate is computed once and forced false for a ledger vacancy, with a logged warn:

```gml
        var __mmapi_npc_roommate = npc.is_roommate(); // mmapi_vacant_roommate_guard
        if (__mmapi_npc_roommate && mmapi_ext_is_vacant("npc_roster", i)) {
            warn("MMAPI: spouse or roommate '{}' is a ledger vacancy - kept parked offscreen until its mod returns", npc_id_to_string(i));
            __mmapi_npc_roommate = false;
        }
```

Both roommate checks in the branch read the guarded variable: schedule selection falls through to the normal per-NPC schedule request (which a vacancy's aldaria-only table turns into the offscreen park), and `roommate_to_sleep` never runs.

## Why

`is_roommate()` and `is_spouse()` read only the `<npc>_status` world fact, which survives an uninstall by design (that persistence is what lets a reinstalled mod resume the marriage). But the engine's new-day roommate branch overrides schedule selection for anyone that fact calls a roommate: it moves them to the player's bed and wakes their brain. For a tombstoned NPC that means spawning in the player's home as an invisible but fully interactive stub, and the roommate routine paths resolve `<npc>_roommate` through a fatal lookup the departed mod can no longer satisfy.

The guard makes vacancy containment win over roommate status without touching the status itself. Zero loss: the fact stays in the save, and the first new day after the mod returns walks the spouse back home.

Zero-registrant inert: the vacant table is empty on an install with no tombstones, so the guarded predicate equals the vanilla one.
