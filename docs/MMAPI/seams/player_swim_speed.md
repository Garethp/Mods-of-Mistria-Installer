# Seam: player_swim_speed

Filters the player's computed swim speed at the return of `get_swim_speed()`.

`player_swim_speed` is a **template seam** (`op = "filter"`). It feeds [player.swim_speed](../hooks/player.swim_speed.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Player/Ari.gml` |
| **Locator** | structural target: `{ fn = "get_swim_speed", at = "before", anchor = "return spd;" }` |
| **Op** | `filter` |
| **Feeds** | [`player.swim_speed`](../hooks/player.swim_speed.md) |
| **Value filtered** | `spd` - the computed swim speed |
| **ctx built** | `{ player: self }` |
| **Marker** | `mmapi_player_swim_speed` |

## The Edit

The generated dispatch lands at the very end of `Ari.get_swim_speed()`, after the fast/slow stroke branch (`HUMAN_SWIM_FAST` with the Hasty infusion and Speedy status effect folded in, or bare `HUMAN_SWIM_SLOW`) and immediately before `return spd;`. It reassigns `spd = mmapi_apply_filters("player.swim_speed", spd, { player: self })` under the uniform try/catch shape, so a filter sees the fully computed swim speed and its return is exactly what the function hands back to the swim states. The Swim state's per-frame movement and the Underwater state's half-speed magnetic pull both call it.

The locator is a structural target rather than a pristine-context anchor: the insertion point is matched token-wise inside `get_swim_speed`, so it is immune to whitespace and comment drift around the return.

## See Also

- [player.swim_speed](../hooks/player.swim_speed.md) - This is the hook this seam dispatches.
- [player_move_speed](player_move_speed.md) - This is a sibling `Ari.gml` filter on the walking/mounted computation.
- [player_mount_speed](player_mount_speed.md) - This is the mounted base-speed dispatch in the same file.
