# Seam: archaeology_dig_artifact

Wraps the artifact roll so every dig spot's yield passes through a filter.

`archaeology_dig_artifact` is a **template seam** (`op = "wrap"`). It feeds [items.dig_artifact](../hooks/items.dig_artifact.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Archaeology.gml` |
| **Locator** | whole-function wrap of `choose_random_artifact()` |
| **Op** | `wrap` |
| **Feeds** | [`items.dig_artifact`](../hooks/items.dig_artifact.md) |
| **Value filtered** | the function's return - the chosen artifact item id |
| **ctx built** | `self` (the `Archaeology` struct) |
| **Marker** | `mmapi_archaeology_dig_artifact` |

## The Edit

A wrap, not an in-body edit: the pristine `choose_random_artifact` is renamed and its body left untouched, and a generated wrapper takes its place under the original name. The wrapper calls the renamed original, runs the result through `mmapi_apply_filters("items.dig_artifact", <result>, self)` in the uniform try/catch shape (catch var `__mmapi_dig_artifact`), and returns whatever comes back: the engine's roll when every handler defers, a replacement item id when one doesn't.

`choose_random_artifact` is the single funnel for archaeology yields (overworld dig locations and dungeon biomes both route through it), so this one wrap covers every dig spot in the game. With zero handlers the wrap is behaviorally (not byte-) equivalent to pristine: the wrapper just forwards the original's return.

## See Also

- [items.dig_artifact](../hooks/items.dig_artifact.md) - This is the hook this seam dispatches.
- [gossip_selections](gossip_selections.md) - This seam uses the same whole-function wrap shape on the gossip picker.
- [items_treasure_distribution_result](items_treasure_distribution_result.md) - This seam is the dungeon treasure roll's result filter.
