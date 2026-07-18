# Seam: npc_receive_gift

Announces every gift the moment an NPC receives it.

`npc_receive_gift` is a **template seam** (`op = "emit"`). It feeds [npc.gift_received](../hooks/npc.gift_received.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/objects/system/parents/par_NPC.gml` |
| **Locator** | pristine context: immediately before `var data = self.me.give_gift(item);` |
| **Op** | `emit` |
| **Feeds** | [`npc.gift_received`](../hooks/npc.gift_received.md) |
| **ctx built** | `{ npc: self, item: item }` |
| **Marker** | `mmapi_npc_receive_gift` |

## The Edit

The generated emit lands at the top of `par_NPC.receive_gift(item)`, anchored by the line that follows it: the seam states only the pristine `var data = self.me.give_gift(item);` as its trailing context, so the dispatch is injected immediately before the NPC's `give_gift` bookkeeping runs. It calls `mmapi_emit("npc.gift_received", { npc: self, item: item })` in the uniform try/catch shape, where `npc` is the NPC instance and `item` the gifted item.

Observation only: the gift proceeds regardless of what handlers do, and the emit fires before any of the engine's gift bookkeeping (heart points, reactions, records) has happened. With zero handlers the emit early-outs on an empty registry, leaving pristine behavior.

## See Also

- [npc.gift_received](../hooks/npc.gift_received.md) - This is the hook this seam dispatches.
- [npc_heart_points](npc_heart_points.md) - This seam is the filter on villager heart-point deltas.
