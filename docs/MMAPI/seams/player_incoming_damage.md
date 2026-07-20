# Seam: player_incoming_damage

Rewrites the player's damage drain so mods filter the final damage and its popup and flinch side effects.

`player_incoming_damage` is a **text seam** (`anchor` + `replace`). It feeds [player.incoming_damage](../hooks/player.incoming_damage.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/objects/characters/obj_ari.gml` |
| **Locator** | text anchor inside the player's damage drain, spanning `took_damage = true;` through the `ARI.modify_health(...)` call |
| **Op** | text (`anchor` + `replace`) |
| **Feeds** | [`player.incoming_damage`](../hooks/player.incoming_damage.md) |
| **Value filtered** | `final_dmg` - the final signed damage, post-mitigation |
| **ctx built** | `{ player: ARI, receiver: self.receiver, tarball: next_dmg.tarball }` |
| **Marker** | `mmapi_player_run_incoming_damage_filters` |

## The Edit

This is the largest text rewrite in the combat set. Pristine code sets `took_damage = true` unconditionally, computes mitigation, and applies the result: `final_dmg = min(-1, defense - next_dmg.tarball.damage)` followed by `ARI.modify_health(final_dmg, ...)`. The replacement reorders and gates every one of those steps:

1. **Mitigation first.** `defense = ARI.get_damage_mitigation()` and `final_dmg = min(-1, defense - next_dmg.tarball.damage)` run exactly as pristine. The filter always sees the post-mitigation number. Note the `min(-1, ...)` clamp: pristine damage is always at most `-1`, so non-negative damage can only come from a filter.
2. **The filter.** `final_dmg = mmapi_apply_filters("player.incoming_damage", final_dmg, { player: ARI, receiver: self.receiver, tarball: next_dmg.tarball })`, in a try/catch that logs a warning on failure.
3. **Popup/flinch flags read off the tarball.** `__mmapi_player_show_damage_popup` and `__mmapi_player_should_flinch` are read from `next_dmg.tarball`, each in its own try/catch, defaulting to `true` when the tarball does not carry them. A [combat.damage](../hooks/combat.damage.md) filter sets these fields earlier in the hit's life. `mmapi_deal_damage()`'s `show_popup` and `flinch` opts ride the same two fields.
4. **The skip.** If `final_dmg >= 0` **and** both flags are `false`, the drain `continue`s. The hit is consumed with no popup, no flinch, and no health change.
5. **Flinch gates `took_damage`.** `took_damage = true` (unconditional in pristine) now runs only while `__mmapi_player_should_flinch` holds, so a no-flinch hit does not trigger the player's flinch reaction.
6. **Health only moves on negative damage.** `is_dead` defaults to `false`, and `ARI.modify_health(final_dmg, damage_was_acid == false)` is called only while `final_dmg < 0`. A filter that returns `0` (or anything non-negative) leaves health untouched even when the popup or flinch still plays.

## See Also

- [player.incoming_damage](../hooks/player.incoming_damage.md) - This is the hook this seam dispatches.
- [combat_damage_pre](combat_damage_pre.md) - This is where a `combat.damage` filter sets the popup/flinch fields this seam reads.
- [player_health_delta](player_health_delta.md) - This is the general filter inside the `modify_health` call this seam gates.
