# Engine Fix: npc_icon_fallback_guard

Keeps the mailbox from crashing when a letter names an NPC the current install does not provide. The sender's icon falls back to Adeline's icon, with a logged warn.

`npc_icon_fallback_guard` is an **engine fix**, a hook-less edit. It dispatches nothing, and there is no handler to register. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/NPCs/NpcUtils.gml` |
| **Locator** | text anchor on the head of `get_npc_icon()` |
| **Op** | text (guarded early return) |
| **Marker** | `mmapi_npc_icon_fallback_guard` |

## The Edit

An unresolved sender returns a fallback icon before the prototype lookup runs:

```gml
function get_npc_icon(npc_id) {
    if (npc_id == undefined) { // mmapi_npc_icon_fallback_guard
        warn("MMAPI: an unresolved npc sender reached get_npc_icon - showing a fallback icon");
        return NPC_PROTOTYPES[NpcId.Adeline].icon_sprite;
    }
```

## Why

Letters resolve their sender with `try_string_to_npc_id`, which yields `undefined` for a name no enum member carries, and nothing validates letter senders at boot. The mailbox then renders each row through `get_npc_icon`, whose bare `NPC_PROTOTYPES[npc_id]` index crashes on `undefined` before the menu appears. A mod letter naming an NPC the install does not provide, whether a typo or a custom NPC whose mod is absent, made the mailbox unopenable.

The engine already drops inbox entries whose whole letter definition is gone at load, so the sender field was the one remaining hole. The guard covers every `get_npc_icon` caller, including the quest log and calendar paths that pass ids from the same resolution family.

Inert on an unmodded install, since every vanilla letter names a real NPC and never reaches the guard.
