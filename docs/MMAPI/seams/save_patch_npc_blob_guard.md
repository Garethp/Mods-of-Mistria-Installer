# Engine Fix: save_patch_npc_blob_guard

Lets a save written before game 1.0.5 pass the 1.0.5 save patch when a custom NPC has no entry in it, skipping that member with a warn instead of refusing the save.

`save_patch_npc_blob_guard` is an **engine fix**, an anchored edit with no hook behind it. Nothing dispatches. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/Serialization/Patches.gml` |
| **Locator** | text anchor: the two lines inside the 1.0.5 patch's NPC loop that fetch a member's blob and read its location |
| **Feeds** | (no hook) |
| **Marker** | `mmapi_save_patch_npc_blob_guard` |

## The Edit

The 1.0.5 patch runs once on any save last written on an older game version. When that save sits on the eve of a festival, the patch walks every `NpcId` member, fetches the member's blob from the npcs file by name, and reads its location on the next line. The anchor is that pair of lines:

```gml
                        var npc_data = npcs[$ npc_id_to_string(i)];
                        var location = string_to_location_id(npc_data.location_position.location_id);
```

The replace inserts a guard between them. A blob that is absent is skipped with a warn naming the member, and the loop moves on:

```gml
                        if npc_data == undefined { // mmapi_save_patch_npc_blob_guard
                            warn("MMAPI: save patch found no blob for {NpcId} - skipped", i);
                            continue;
                        }
```

A vanilla save carries a blob for every member of the vanilla roster, so on an unmodded install the guard never fires. A custom NPC, live or vacant, has no blob in a save written before it existed. Without the guard the read throws, the patcher's own catch logs the error, and the title menu reports the save as invalid until the mod is removed. With the guard the patch completes, writes the npcs file back without an entry for that member, and the load-side [npc_load_missing_blob_guard](npc_load_missing_blob_guard.md) then handles the missing blob as it does for every other load.

## See Also

- [npc_load_missing_blob_guard](npc_load_missing_blob_guard.md) - The load-side guard for the same missing blob, which runs only after the patcher has let the save through.
- [vacant_roommate_load_guard](vacant_roommate_load_guard.md) - The other load-side guard a vacant member needs.
- [npc_roster](../extensions/npc_roster.md) - The extension point whose members this guard protects.
