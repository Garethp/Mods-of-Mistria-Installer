# Seam: player_equipment_bonus

Rewrites the equipment bonus lookup's return into a filtered return.

`player_equipment_bonus` is a **text seam** (`anchor` + `replace`). It feeds [player.equipment_bonus](../hooks/player.equipment_bonus.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Player/Ari.gml` |
| **Locator** | text anchor: the `return value;` of the equipment bonus lookup, pinned by the `get_damage_mitigation()` declaration that follows it |
| **Op** | text (`anchor` + `replace`) |
| **Feeds** | [`player.equipment_bonus`](../hooks/player.equipment_bonus.md) |
| **Value filtered** | `value` - the computed equipment bonus |
| **ctx built** | `{ player: self, infusion: infusion, key: key }` |
| **Marker** | `mmapi_player_run_equipment_bonus_filters` |

## The Edit

The seam replaces the lookup's bare `return value;` with `return mmapi_apply_filters("player.equipment_bonus", value, { player: self, infusion: infusion, key: key });`. The function's whole answer now flows through the filter chain on its way out. The engine computes the bonus exactly as before. The filter sees the finished number plus the lookup's own arguments (`infusion`, `key`) and the player (`self`), and every caller of the lookup (`get_damage_mitigation()` among them) receives the post-filter value.

`return value;` is not a unique line in `Ari.gml`, so the anchor spans past the closing brace to the `function get_damage_mitigation() {` declaration that follows. That trailing context is what pins the edit to this one return. The dispatch is inline in the return expression with no seam-level try/catch. The filter registry's own per-handler isolation is what contains a throwing handler.

## See Also

- [player.equipment_bonus](../hooks/player.equipment_bonus.md) - This is the hook that this seam dispatches.
- [player_incoming_damage](player_incoming_damage.md) - This is where `get_damage_mitigation()`, the lookup's neighbor, feeds into damage.
- [player_move_speed](player_move_speed.md) - This seam is another `Ari.gml` stat filter.
