# Seam: npc_is_unlocked_vacancy

Makes `npc_is_unlocked()`'s default arm vacancy-aware and mod-filterable.

`npc_is_unlocked_vacancy` is a **text seam** (filter-shaped: it dispatches `mmapi_apply_filters`). It feeds [npc.is_unlocked](../hooks/npc.is_unlocked.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/NPCs/Npc.gml` |
| **Locator** | text anchor on the switch's last vanilla case (`NpcId.Seridia`) plus the `default: return true;` arm it replaces |
| **Op** | text (filter dispatch) |
| **Feeds** | [`npc.is_unlocked`](../hooks/npc.is_unlocked.md) |
| **Value filtered** | the unlocked boolean, starting at `!mmapi_ext_is_vacant("npc_roster", npc_id)` |
| **ctx built** | `npc_id` (the ordinal) |
| **Marker** | `mmapi_npc_is_unlocked` |

## The Edit

The default arm becomes:

```gml
default:
    var __mmapi_npc_unlocked = !mmapi_ext_is_vacant("npc_roster", npc_id); // mmapi_npc_is_unlocked
    __mmapi_npc_unlocked = mmapi_apply_filters("npc.is_unlocked", __mmapi_npc_unlocked, npc_id);
    return __mmapi_npc_unlocked;
```

## Why

The relationships journal filters every row through `npc_is_unlocked` *before* reading the NPC's data. A ledger vacancy, meaning an [extension NPC](../EXTENSIONS.md) whose mod was uninstalled, would otherwise leave a ghost row backed by stub data. Starting the value at "not vacant" hides tombstones. The filter lets a mod gate its own NPC behind progress, or deliberately show a departed villager instead.

With zero handlers and zero vacancies the arm returns `true`, behaviorally identical to pristine. Vanilla NPCs with explicit cases never reach the default arm, so the base game's own gating (market vendors, story NPCs) is untouched.
