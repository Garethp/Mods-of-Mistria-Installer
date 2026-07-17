# Hook: npc.gift_received

Know when the player gives an NPC a gift.

`npc.gift_received` is an **event** hook. Register a callback with `mmapi_on`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires at the top of `par_NPC.receive_gift(item)`, when the player gives an NPC a gift, before the NPC's `give_gift` bookkeeping. ctx is `{ npc, item }`: `npc` is the NPC instance, `item` is the gifted item. This hook is observation only.

| | |
| --- | --- |
| **Fires** | At the top of `par_NPC.receive_gift(item)`, before the NPC's `give_gift` bookkeeping. |
| **ctx** | `{ npc, item }` |
| **Kind contract** | The callback observes the moment. Its return value is ignored. |

### The ctx struct

- `npc` - The NPC instance receiving the gift (the `par_NPC` object). Its data struct is `npc.me`, whose `give_gift(item)` runs right after the emit.
- `item` - the gifted item.

## Usage

```gml
// npc.gift_received is an EVENT: mmapi calls you after it happens.
// You cannot change or stop it here; the return value is ignored.
function gift_ledger_npc_gift_received(_ctx) {
    // _ctx is { npc, item }.
    //   .npc  - the NPC instance receiving the gift.
    //   .item - the gifted item.
    // The give_gift bookkeeping has not run yet.
    // your code here
}

// inside your latched register function (see Mod Anatomy):
mmapi_on("npc.gift_received", gift_ledger_npc_gift_received);
```

## Engine Wiring

- Seam [`npc_receive_gift`](../seams/npc_receive_gift.md) dispatches from `gml/objects/system/parents/par_NPC.gml`, immediately before `self.me.give_gift(item)` in `receive_gift()`.

## See Also

- [npc.heart_points](npc.heart_points.md) - Adjust the heart points a villager gains.
- [gossip.selections](gossip.selections.md) - Change which NPCs the day's gossip offers.
- [items.give](items.give.md) - Rewrite any item the player is about to receive.
