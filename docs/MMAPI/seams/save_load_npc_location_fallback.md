# Engine Fix: save_load_npc_location_fallback

Sends an NPC flagged by the location half to Aldaria on the basement schedule, the engine's own recovery for a lost schedule.

`save_load_npc_location_fallback` is an **engine fix**, an anchored edit with no hook behind it. Nothing dispatches. It is the fallback half of the NPC location pair, with [save_load_npc_location_tolerance](save_load_npc_location_tolerance.md) as the location half that sets the flag this edit reads. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/NPCs/Npc.gml` |
| **Locator** | text anchor on the `schedule_reload` call and its `undefined` test in `deserialize` |
| **Feeds** | (no hook) |
| **Depends on** | [`save_load_npc_location_tolerance`](save_load_npc_location_tolerance.md) |
| **Marker** | `mmapi_save_npc_location_fallback` |

## The Edit

The replace makes the schedule reload conditional on the location half's flag and adds a branch ahead of the engine's own fallback. The `T2R.schedule_reload` call, with the marker on its line, yields `undefined` without running when the flag is set. A new branch on the flag then starts `Schedules/basement_schedule` for the NPC and places it at the `default` trellis point of `rm_aldaria`, and the engine's original `output == undefined` branch follows as an `else if`.

`Npc.deserialize` already recovers from a saved schedule that no longer exists. It reports the missing schedule, starts the basement schedule and places the NPC in Aldaria. That is also the right outcome for an NPC whose saved location no longer exists, because resuming its saved schedule from Aldaria would pathfind it out along an itinerary built for the missing room, and the engine's map pathfinder crashes on a route it cannot build.

The fix takes the same two steps for the flagged NPC and skips the reload, so the NPC is in the state the engine's own recovery produces. It does not raise the engine's missing schedule report, since the schedule itself was found. The NPC spends the rest of the day in Aldaria and resumes its normal schedule at the next day start, which is how the engine's own fallback behaves. The saved world facts are left to the runtime, which rewrites them as the schedule advances.

The flag is a local the location half declares, so the two edits apply together or not at all, and the catalog declares the dependency. When the flag is clear, the reload runs with the same arguments and the original branch follows, so an intact install behaves exactly as before.

## See Also

- [save_load_npc_location_tolerance](save_load_npc_location_tolerance.md) - The location half of the pair.
- [save_load_renown_list_tolerance](save_load_renown_list_tolerance.md) - Another fix in the family built as a pair.
