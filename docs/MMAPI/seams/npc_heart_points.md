# Seam: npc_heart_points

Reroutes every villager heart-point delta through a filter before it lands.

`npc_heart_points` is a **text seam** (filter-shaped: it dispatches `mmapi_apply_filters`). It feeds [npc.heart_points](../hooks/npc.heart_points.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/NPCs/Npc.gml` |
| **Locator** | text anchor at the head of `Npc.add_heart_points(amount)`, right after `var old = self.heart_level();` |
| **Op** | text (filter dispatch) |
| **Feeds** | [`npc.heart_points`](../hooks/npc.heart_points.md) |
| **Value filtered** | `amount` - the heart points delta |
| **ctx built** | `self` (the `Npc` struct) |
| **Marker** | `mmapi_npc_run_heart_filters` |

## The Edit

The seam re-states the head of `add_heart_points(amount)` (the signature plus its `var old = self.heart_level();` line) and appends one injected line:

```gml
amount = mmapi_apply_filters("npc.heart_points", amount, self); // mmapi_npc_run_heart_filters
```

The filter runs after the engine snapshots the pre-change heart level into `old` and before the delta is applied, so the pristine before/after comparison still works while the delta itself is whatever the filter chain returns. ctx is `self`, the `Npc` struct, letting a handler scope its change to specific villagers. This seam is distinct from `animal_heart_points`, which covers barn animals rather than villagers. The injected line is a bare reassignment with no try/catch of its own. Per-handler isolation lives inside the registry dispatch, and with zero handlers the value passes through unchanged.

## See Also

- [npc.heart_points](../hooks/npc.heart_points.md) - This is the hook that this seam dispatches.
- [animal_heart_points](animal_heart_points.md) - This is the same edit on barn-animal heart points in `Animal.add_heart_points()`.
- [npc_receive_gift](npc_receive_gift.md) - This is the event emit when the player gives an NPC a gift.
