# Seam: player_status_effect_register

Filters every status effect's fields at the top of `register()`.

`player_status_effect_register` is a **text seam** (`anchor` + `replace`). It feeds [player.status_effect_register](../hooks/player.status_effect_register.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/Player/StatusEffectManager.gml` |
| **Locator** | text anchor: the head of `register(type, amount, start, finish, can_stack=false, show_hud=true)`, before the `effect` struct is built |
| **Op** | text (`anchor` + `replace`) |
| **Feeds** | [`player.status_effect_register`](../hooks/player.status_effect_register.md) |
| **Value filtered** | `{ type, amount, start_time, finish_time, can_stack, show_hud }` |
| **ctx built** | `self` - the `StatusEffectManager` |
| **Marker** | `mmapi_player_run_status_register_filters` |

## The Edit

The injected block builds the value struct from `register()`'s six parameters (`{ type: type, amount: amount, start_time: start, finish_time: finish, can_stack: can_stack, show_hud: show_hud }`) and runs it through `mmapi_apply_filters("player.status_effect_register", ..., self)` inside a try/catch, with the manager itself as ctx.

The write-back is fully defensive: when the result is not `undefined`, each parameter is re-read from the struct in its own try/catch (`type`, `amount`, `start` from `start_time`, `finish` from `finish_time`, `can_stack`, `show_hud`), one at a time. A handler that returns a partial struct, mutates in place and returns `undefined`, or returns something malformed rewrites only the fields it actually carries. Nothing it does can crash registration. Control then falls into the pristine `var effect = {` construction with the possibly-rewritten values, so the filtered fields are what the manager stores, times out, and shows on the HUD.

## See Also

- [player.status_effect_register](../hooks/player.status_effect_register.md) - This is the hook this seam dispatches.
- [player_status_effect_expired](player_status_effect_expired.md) - This is the emit when a registered effect times out.
- [player_status_effect_cancel](player_status_effect_cancel.md) - This is the emit when an effect is cancelled early.
