# Engine Fix: npc_custom_actor_resolution

Resolves a registered custom NPC id as a Mist cutscene actor.

`npc_custom_actor_resolution` is an **engine fix**, an anchored edit with no hook behind it - `mmapi_register_custom_npc_id` (`mmapi_npc.gml`) is the mod-facing API, not a hook to register against. See [Seams](../SEAMS.md) and [Registering a Custom NPC](../RECIPES.md#register-a-custom-npc-id).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Mist/Std.gml` |
| **Locator** | text anchor: `actor_name_to_object`'s `try_string_to_npc_id` branch |
| **Feeds** | (no hook) |
| **Marker** | `mmapi_npc_custom_actor_resolution` |

## The Edit

`actor_name_to_object` is the one function every `.mist` script command routes an actor name through. Its native-NPC branch already gates on `try_string_to_npc_id`, so this fix adds a new branch after it, checked only once the native resolution has already failed:

```gml
} else if try_string_to_npc_id(_name) != undefined {
    return npc_id_to_gm_obj_id(string_to_npc_id(_name));
} else if (__mmapi_custom_npc_object_for(_name) != undefined) {
    return __mmapi_custom_npc_object_for(_name);
} else {
    return cameo_id_to_gm_obj_id(string_to_cameo_id(_name));
}
```

A real vanilla name is unaffected - it resolves through the native branch exactly as before and never reaches the new check. `npc_id_to_gm_obj_id` is never called with a custom id, so its `impossible()` crash on an unrecognized value never fires. See [npc_custom_quest_query](npc_custom_quest_query.md) for why this is a targeted edit and not a `[[call_rewrite]]` on `string_to_npc_id`.

## See Also

- [npc_custom_await_animation](npc_custom_await_animation.md), [npc_custom_shadow_enabled](npc_custom_shadow_enabled.md), [npc_custom_entry_transition](npc_custom_entry_transition.md), [npc_custom_door_use](npc_custom_door_use.md) - The other four Mist call sites patched the same way.
- [npc_custom_quest_query](npc_custom_quest_query.md) - The one non-Mist call site, and the full rationale for six targeted edits instead of one call rewrite.
