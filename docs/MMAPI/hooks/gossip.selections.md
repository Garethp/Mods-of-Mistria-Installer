# Hook: gossip.selections

Change which NPCs the day's gossip offers.

`gossip.selections` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires at the return of `get_gossip_selections()`, the NPCs offered for the day's gossip. The filtered value is that selection list. ctx is `undefined`. Return a replacement list (for example an empty list to suppress gossip), or `undefined` to keep the current value.

| | |
| --- | --- |
| **Fires** | At the return of `get_gossip_selections()`. |
| **Value** | The day's gossip NPC selection list. |
| **ctx** | `undefined` |
| **Kind contract** | The callback receives the current value and returns a replacement, or `undefined` to keep the current value. |

## Usage

```gml
// gossip.selections is a FILTER: you receive (value, ctx) and return a
// replacement, or undefined to keep the game's value.
function quiet_town_gossip_selections(_value, _ctx) {
    // _value is the day's gossip NPC selection list.
    // _ctx is undefined.
    if (_value == undefined) return undefined; // test undefined BEFORE anything else
    // return an empty list to suppress gossip entirely, or a modified
    // list to change who is offered:
    return undefined; // undefined = keep the game's value
}

mmapi_filter("gossip.selections", quiet_town_gossip_selections);
```

## Engine Wiring

- Seam [`gossip_selections`](../seams/gossip_selections.md) dispatches from `gml/scripts/GossipMenu.gml`, a whole-function wrap of `get_gossip_selections()` that filters its return value.

## See Also

- [npc.heart_points](npc.heart_points.md) - Adjust the heart points a villager gains.
- [npc.gift_received](npc.gift_received.md) - Know when the player gives an NPC a gift.
- [dialogue.path](dialogue.path.md) - Redirect a conversation before it plays.
