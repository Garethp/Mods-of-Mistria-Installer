# Engine Fix: npc_load_missing_blob_guard

Lets a pre-existing save load after a custom NPC is installed, and lets a save that *knows* a custom NPC load after that NPC's mod is uninstalled.

`npc_load_missing_blob_guard` is an **engine fix**, a hook-less edit. It dispatches nothing, and there is no handler to register. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Cycle/LoadGame.gml` |
| **Locator** | text anchor on the per-NPC deserialize loop |
| **Op** | text (guard + placement) |
| **Marker** | `mmapi_npc_load_guard` |

## The Edit

```gml
var __mmapi_npc_blob = files.npcs[$ npc_id_to_string(i)]; // mmapi_npc_load_guard
if (mmapi_ext_is_vacant("npc_roster", i)) {
    if (__mmapi_npc_blob != undefined) {
        npc.heart_points = __mmapi_npc_blob.heart_points;
        npc.talk_flag = __mmapi_npc_blob.talk_flag;
        npc.gift_flag = __mmapi_npc_blob.gift_flag;
        npc.times_spoken_today = __mmapi_npc_blob.times_spoken_today;
        npc.known_gift_preferences = HashSetFromArray(array_map(__mmapi_npc_blob.known_gift_preferences, string_to_item_id_or_unknown));
        npc.gifts_given = HashSetFromArray(array_map(__mmapi_npc_blob.gifts_given, string_to_item_id_or_unknown));
    }
    npc.location_position = new LocationPosition(LocationId.Aldaria, Vec2(0, 0));
    npc.brain_dead = true;
}
else {
    if (__mmapi_npc_blob != undefined) { npc.deserialize(__mmapi_npc_blob); }
    else { npc.location_position = new LocationPosition(LocationId.Town, Vec2(0, 0)); }

    npc.brain_dead = T2R.schedule_current_action_has_arrived(i);
}
```

## Why

LoadGame walks the current enum and deserializes each NPC's save blob with no guard on the lookup. Two directions can go wrong.

**The save predates the NPC**, meaning the mod is newly installed. The unguarded lookup yields `undefined` and `deserialize` dereferences it immediately, so installing any NPC mod would brick every pre-existing save. The guard skips the absent blob, and the fresh Npc keeps its defaults of zero hearts and never met. The guard also parks the NPC. The Npc constructor deliberately leaves `location_position` undefined, the roster machinery dereferences it unguarded on every-ordinal scans, and a skipped but unplaced NPC therefore crashes the first activity tick after load. A live NPC parks in Town, which is walk-connected, and the next day-start snap restores its schedule.

**The save outlives the NPC**, meaning the mod was uninstalled and the ordinal is now a vacancy. The blob exists but describes departed content. A full `Npc.deserialize` resolves that content: `wardrobe.set_outfit` and `set_animation` walk the mod's departed sprite sets, and the closing native `T2R.schedule_reload` aborts the entire load on the departed schedule path, silently. So a vacancy never deserializes. It restores social data only, meaning heart points, gift history, and daily flags, through the engine's own tolerant `string_to_item_id_or_unknown`. It parks in the Aldaria pen, which is deliberately walk-disconnected, and it is `brain_dead = true` by definition, skipping the t2 schedule query a vacancy has no answer for.

A save made while vacant serializes that parked state. Reinstalling the mod makes the NPC live again. Hearts and gift history carry through, and the first day-start snap walks it back onto its real schedule.

Zero-registrant inert: a same-version vanilla save has a blob for every vanilla NPC and no vacancies, so only the `else` branch runs, and behavior is equivalent to vanilla. Game-version additions are handled upstream by the save-migration patches.
