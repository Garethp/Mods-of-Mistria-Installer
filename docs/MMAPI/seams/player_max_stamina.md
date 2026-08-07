# Seam: player_max_stamina

Filters the player's maximum stamina calculation.

`player_max_stamina` is a **text seam** (`anchor` + `replace`). It feeds [player.max_stamina](../hooks/player.max_stamina.md). Mod authors register handlers for the hook; they do not write seams. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Player/Ari.gml` |
| **Locator** | the complete `Ari.get_max_stamina()` function |
| **Op** | text (`anchor` + `replace`) |
| **Feeds** | [`player.max_stamina`](../hooks/player.max_stamina.md) |
| **Value filtered** | base stamina plus the Tireless equipment bonus |
| **Context** | `{ player: self }` |
| **Marker** | `mmapi_player_run_max_stamina_filters` |

## Behavior

The replacement preserves the original calculation, applies the filter, accepts only numeric results, and clamps accepted negative results to zero. Dispatch failures leave the engine value unchanged. With zero handlers, the result is behaviorally equivalent to the pristine function.
