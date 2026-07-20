# Seam: ui_button_sprites

Puts a filter on each built button sprite set before it enters the cache.

`ui_button_sprites` is a **template seam** (`op = "filter"`). It feeds [ui.button_sprites](../hooks/ui.button_sprites.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/UI/Anchor/anchor_utils.gml` |
| **Locator** | pristine context immediately before `BUTTON_SPRITE_CACHE.insert(key, output)` |
| **Op** | `filter` |
| **Feeds** | [`ui.button_sprites`](../hooks/ui.button_sprites.md) |
| **Value filtered** | `output`, the built button sprite set |
| **ctx built** | `{ key: key }` |
| **Marker** | `mmapi_ui_run_button_sprites_filters` |

## The Edit

The generated dispatch reassigns `output = mmapi_apply_filters("ui.button_sprites", output, { key: key })` in the uniform try/catch shape, just before the builder inserts the result into `BUTTON_SPRITE_CACHE` and returns it. Because the filtered value is what gets cached, a handler runs exactly once per cache key, and its replacement is what every later lookup for that key serves. Cache hits are never re-filtered.

## See Also

- [ui.button_sprites](../hooks/ui.button_sprites.md) - This is the hook this seam dispatches.
- [ui_item_node_set_to_item](ui_item_node_set_to_item.md) - This is a sibling seam in `anchor_utils.gml`, for item nodes.
- [ui_hud_should_show](ui_hud_should_show.md) - This is a sibling seam in `anchor_utils.gml`, for HUD visibility.
