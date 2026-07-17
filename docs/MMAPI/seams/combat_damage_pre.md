# Seam: combat_damage_pre

Threads every enqueued hit through a damage filter before it resolves.

`combat_damage_pre` is a **text seam** (`anchor` + `replace`). It feeds [combat.damage](../hooks/combat.damage.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/objects/Combat/obj_damage_receiver.gml` |
| **Locator** | text anchor inside the receiver's damage drain loop, spanning the dequeue and the iframe/rockstack checks |
| **Op** | text (`anchor` + `replace`) |
| **Feeds** | [`combat.damage`](../hooks/combat.damage.md) |
| **Value filtered** | `tarball` - the damage tarball instance |
| **ctx built** | `self` - the damage receiver |
| **Marker** | `mmapi_combat_run_damage_filters` |

## The Edit

The replacement re-emits the drain's pristine head verbatim: the tarball is dequeued from `self.damaged`, a destroyed tarball is skipped, the iframe check runs (`current_iframes > 0` on a non-`Acid` hit removes the receiver from `tarball.already_hit_array` and continues), and the rockstack check runs (a receiver that is not `obj_monster_rock_stack` skips `Rockstack`-flagged tarballs). Only after all three pristine gates pass does the inserted dispatch run: `tarball = mmapi_apply_filters("combat.damage", tarball, self)`, wrapped in a try/catch that logs a warning instead of crashing the drain.

The post-filter tarball is then re-checked: if it comes back `undefined` or destroyed, the loop `continue`s and the hit is skipped entirely. That gives a filter three moves: return a replacement tarball, mutate the one it was given, or kill the hit outright (return `undefined`, or destroy the instance). Fields a filter sets on the tarball are read later the same frame by the [player_incoming_damage](player_incoming_damage.md) seam (`__mmapi_player_show_damage_popup`, `__mmapi_player_should_flinch`). Hits injected via `mmapi_deal_damage()` carry `__mmapi_injected` with the injecting mod's name. Engine hits do not.

## See Also

- [combat.damage](../hooks/combat.damage.md) - This is the hook this seam dispatches.
- [combat_damage_resolved](combat_damage_resolved.md) - This seam emits on the resolution side in the same drain.
- [player_incoming_damage](player_incoming_damage.md) - This seam reads the popup/flinch fields a `combat.damage` filter sets on the tarball.
- [combat_tarball_grid](combat_tarball_grid.md) - This seam is the tarball's grid-interaction filter, earlier in the tarball's life.
