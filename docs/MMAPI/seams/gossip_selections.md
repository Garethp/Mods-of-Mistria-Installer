# Seam: gossip_selections

Wraps the gossip picker so the day's NPC selection passes through a filter.

`gossip_selections` is a **template seam** (`op = "wrap"`). It feeds [gossip.selections](../hooks/gossip.selections.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GossipMenu.gml` |
| **Locator** | whole-function wrap of `get_gossip_selections()` |
| **Op** | `wrap` |
| **Feeds** | [`gossip.selections`](../hooks/gossip.selections.md) |
| **Value filtered** | the function's return - the NPCs offered for the day's gossip |
| **ctx built** | `undefined` |
| **Marker** | `mmapi_gossip_selections` |

## The Edit

A wrap, not an in-body edit: the pristine `get_gossip_selections` is renamed and its body left untouched, and a generated wrapper takes its place under the original name. The wrapper calls the renamed original, runs the selection list through `mmapi_apply_filters("gossip.selections", <result>, undefined)` in the uniform try/catch shape (catch var `__mmapi_gossip`), and returns whatever comes back. ctx is `undefined`. The selection list itself is the whole story. A handler can return a replacement list, including an empty list to suppress gossip for the day.

With zero handlers the wrap is behaviorally (not byte-) equivalent to pristine: the wrapper just forwards the original's return.

## See Also

- [gossip.selections](../hooks/gossip.selections.md) - This is the hook that this seam dispatches.
- [archaeology_dig_artifact](archaeology_dig_artifact.md) - This seam uses the same whole-function wrap shape on the artifact roll.
- [npc_receive_gift](npc_receive_gift.md) - This is another NPC-social seam that announces gifts.
