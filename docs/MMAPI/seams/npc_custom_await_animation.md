# Engine Fix: npc_custom_await_animation

Resolves a registered custom NPC id in the Mist `__await_npc_animation` command.

`npc_custom_await_animation` is an **engine fix**, an anchored edit with no hook behind it. See [npc_custom_actor_resolution](npc_custom_actor_resolution.md) for the full rationale shared by all six of these edits, and [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Mist/Std.gml` |
| **Locator** | text anchor: `__await_npc_animation`'s resolve line |
| **Feeds** | (no hook) |
| **Marker** | `mmapi_npc_custom_await_animation` |

## The Edit

`__await_npc_animation` resolves its `npc` parameter to an object before reading its animator:

```gml
npc = __mmapi_custom_npc_object_for(npc) ?? npc_id_to_gm_obj_id(string_to_npc_id(npc));
```

A registered custom id resolves via the registry; anything else falls through to the native chain unchanged.

## See Also

- [npc_custom_actor_resolution](npc_custom_actor_resolution.md) - The core Mist actor chokepoint, and the full rationale for these six targeted edits over a single call rewrite.
