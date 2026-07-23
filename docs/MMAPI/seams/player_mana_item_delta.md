# Seam: player_mana_item_delta

Reroutes the mana potion's direct `set_mana` call through `modify_mana`, so item restores fire the mana filter.

`player_mana_item_delta` is a **text seam** (`anchor` + `replace`). It feeds [player.mana_delta](../hooks/player.mana_delta.md), though it dispatches nothing itself - the dispatch lives in [player_mana_delta](player_mana_delta.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/Player/AriFsm.gml` |
| **Locator** | text anchor on the mana potion's consume line, `ARI.set_mana(ARI.mana_current + ...mana_modifier)` |
| **Op** | text (`anchor` + `replace`) |
| **Feeds** | [`player.mana_delta`](../hooks/player.mana_delta.md) (via [`player_mana_delta`](player_mana_delta.md)) |
| **Value filtered** | none - this edit dispatches nothing itself |
| **ctx built** | none |
| **Marker** | `mmapi_player_mana_item_delta` |

## The Edit

A one-line reroute. When the player drinks a mana potion, pristine code credits the restore with `ARI.set_mana(ARI.mana_current + self.live_item.prototype.mana_modifier)` - the only gameplay-delta mana change in the engine that bypasses `modify_mana`. The replacement is `ARI.modify_mana(self.live_item.prototype.mana_modifier)`.

The reroute is behavior-identical: `modify_mana(x)` is literally `set_mana(get_mana() + x)`, and `get_mana()` returns `mana_current`. What it buys is coverage - the potion's restore now enters the same funnel every other mana delta uses, so a [player.mana_delta](../hooks/player.mana_delta.md) handler sees item restores without a second dispatch site. This is the same companion-edit pattern as [dialogue_speaker_ctx_arg](dialogue_speaker_ctx_arg.md): an edit that dispatches nothing of its own, existing only so its sibling's dispatch sees everything.

## See Also

- [player.mana_delta](../hooks/player.mana_delta.md) - This is the hook this reroute feeds.
- [player_mana_delta](player_mana_delta.md) - This is the dispatch the rerouted call now reaches.
- [dialogue_speaker_ctx_arg](dialogue_speaker_ctx_arg.md) - The other dispatch-less companion edit in the catalog.
