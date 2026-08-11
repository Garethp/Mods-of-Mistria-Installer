# Engine Fix: npc_custom_shadow_enabled

Resolves a registered custom NPC id in the Mist `set_shadow_enabled` command.

`npc_custom_shadow_enabled` is an **engine fix**, an anchored edit with no hook behind it. See [npc_custom_actor_resolution](npc_custom_actor_resolution.md) for the full rationale shared by all six of these edits, and [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Mist/Std.gml` |
| **Locator** | text anchor: `set_shadow_enabled`'s resolve line |
| **Feeds** | (no hook) |
| **Marker** | `mmapi_npc_custom_shadow_enabled` |

## The Edit

`set_shadow_enabled` resolves its `npc` parameter inline before reading `.shadow_caster`:

```gml
shadow_caster_set_alpha((__mmapi_custom_npc_object_for(npc) ?? npc_id_to_gm_obj_id(string_to_npc_id(npc))).shadow_caster, boo);
```

A registered custom id resolves via the registry; anything else falls through to the native chain unchanged.

## See Also

- [npc_custom_actor_resolution](npc_custom_actor_resolution.md) - The core Mist actor chokepoint, and the full rationale for these six targeted edits over a single call rewrite.
