# Engine Fix: npc_custom_quest_query

Resolves a registered custom NPC id in a quest's `QuestQueryType.Npc` query.

`npc_custom_quest_query` is an **engine fix**, an anchored edit with no hook behind it - `mmapi_register_custom_npc_id` (`mmapi_npc.gml`) is the mod-facing API. See [Seams](../SEAMS.md) and [Registering a Custom NPC](../RECIPES.md#register-a-custom-npc-id).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Quests/QuestDatabase.gml` |
| **Locator** | text anchor: `parse_query`'s `QuestQueryType.Npc` case, the `npc_name` resolve line |
| **Feeds** | (no hook) |
| **Marker** | `mmapi_npc_custom_quest_query` |

## The Edit

Quest TOMLs parse once at boot, during `Setup`'s boot-time `create` event - **before the `Game` object exists**, and therefore before `mmapi_run_installs()` (wired to `Game`'s `step_begin`, see [game_step_begin_installs](game_step_begin_installs.md)) has ever drained a deferred `mmapi_register(...)` installer. `mmapi_register_custom_npc_id` must be called at a mod's top-level boot for a custom id to be resolvable here - see the warning in [Registering a Custom NPC](../RECIPES.md#register-a-custom-npc-id). A registration made the normal deferred way simply doesn't exist yet when this code runs.

`parse_query`'s `Npc` case resolved `f_query.npc` with a single unguarded `string_to_npc_id` call; this fix tries a real name, then the custom registry, before falling back to the native call (preserving the original crash-on-garbage-name behavior for a genuinely malformed quest):

```gml
var npc_name = try_string_to_npc_id(f_query.npc) ?? __mmapi_custom_npc_object_for(f_query.npc) ?? string_to_npc_id(f_query.npc);
```

The stored `npc_name` is later compared for equality in `par_NPC.gml`'s `my_query_quests()` - but that method only runs on `par_NPC` instances, which a custom NPC does not inherit from. Matching a quest to a custom NPC's own turn-in code is the consuming mod's responsibility, same as any `par_NPC`-only behavior already is for real NPCs. `mmapi_register_custom_npc_id` (`mmapi_npc.gml`) rejects any `object_index` inside the real `NpcId` enum's own numeric range specifically so a stored custom `npc_name` can never coincide with an unrelated real NPC's id in that `==` comparison.

## Why an engine fix, not a `[[call_rewrite]]`

`string_to_npc_id`/`try_string_to_npc_id` are native builtins with no GML body, so a `[[call_rewrite]]` (like [local_get_dispatch](local_get_dispatch.md)) can redirect every call site at once. That was the first design considered here, and it's wrong for this case: the two natives have roughly 30 other call sites across the engine, several of which index straight into the fixed-size `NPCS[]`/`NPC_PROTOTYPES[]` arrays with the raw result (`Std.gml`'s `__mark_actor_as_met` and `__request_music_play`, plus sites in `BuggerInitialize.gml`, `Dates.gml`, `NpcDatabase.gml`, `Inbox.gml`, `LoadGame.gml`). A tree-wide rewrite would hand every one of those a custom-id value too, and none of them can handle anything but a real `NpcId`. This edit, and its five siblings ([npc_custom_actor_resolution](npc_custom_actor_resolution.md) and friends), instead patch only the specific call sites that should understand a custom id, leaving every other call site - and the real `NpcId` enum and `NPCS[]` array themselves - completely untouched.

## See Also

- [npc_custom_actor_resolution](npc_custom_actor_resolution.md) - The Mist cutscene-actor counterpart to this quest-side edit.
- [local_get_dispatch](local_get_dispatch.md) - The catalog's one call rewrite, and the shape this feature deliberately did not take.
