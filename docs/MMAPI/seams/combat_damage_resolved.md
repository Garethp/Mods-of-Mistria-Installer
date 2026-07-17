# Seam: combat_damage_resolved

Announces the outcome of every hit the receiver's resolution switch lands or blocks.

`combat_damage_resolved` is a **text seam** (`anchor` + `replace`). It feeds [combat.damage_resolved](../hooks/combat.damage_resolved.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/objects/Combat/obj_damage_receiver.gml` |
| **Locator** | text anchor spanning the receiver's whole `switch self.status` resolution block |
| **Op** | text (`anchor` + `replace`) |
| **Feeds** | [`combat.damage_resolved`](../hooks/combat.damage_resolved.md) |
| **ctx built** | `{ receiver: self, tarball: tarball, status: self.status, successful: <per branch> }` |
| **Marker** | `mmapi_combat_run_damage_hit_callbacks` |

## The Edit

The replacement re-emits the receiver's resolution switch with an emit inserted at each branch where a hit actually resolves. Three emits cover four outcomes:

- `ReceiverStatus.Normal` and `ReceiverStatus.DamageOnAttack` share one case body: the emit lands right after `tarball.succesful_hit(self)`, with `successful: true`.
- `ReceiverStatus.Blocking`: the emit lands after `tarball.blocked()` and before the receiver's iframes are set, with `successful: false`.
- `ReceiverStatus.Aerial`: only the `CombatFlag.InAir` branch emits, after `tarball.succesful_hit(self)`, with `successful: true`. A grounded hit against an aerial receiver takes the `else` branch, sets `cont = true`, and never emits.

`ReceiverStatus.UntimedInvulnerable` never emits either: the hit is discarded (`cont = true`) and the receiver removed from `tarball.already_hit_array`. Each emit is wrapped in its own try/catch that logs a warning, so a throwing handler cannot derail the switch. Note the ctx field is spelled `successful` even though the engine method it follows is the misspelled `succesful_hit`.

## See Also

- [combat.damage_resolved](../hooks/combat.damage_resolved.md) - This is the hook that this seam dispatches.
- [combat_damage_pre](combat_damage_pre.md) - This seam is the pre-resolution filter in the same drain.
- [combat_tarball_grid](combat_tarball_grid.md) - This seam is the tarball-side filter that runs before hits are enqueued.
