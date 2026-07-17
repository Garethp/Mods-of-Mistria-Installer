# Seam: ui_hud_should_show

Wraps `hud_should_show()` so mods get the last word on HUD visibility.

`ui_hud_should_show` is a **template seam** (`op = "wrap"`). It feeds [ui.hud_should_show](../hooks/ui.hud_should_show.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/UI/Anchor/anchor_utils.gml` |
| **Locator** | whole-function wrap of `hud_should_show()` |
| **Op** | `wrap` |
| **Feeds** | [`ui.hud_should_show`](../hooks/ui.hud_should_show.md) |
| **Value filtered** | the boolean `hud_should_show()` computes |
| **ctx built** | `undefined` |
| **Marker** | `mmapi_ui_run_hud_should_show_filters` |

## The Edit

A wrap targets the whole function: the pristine `hud_should_show` definition is renamed, its body untouched, and a generated wrapper takes its place. It calls the renamed original and filters the computed boolean through `mmapi_apply_filters("ui.hud_should_show", <return>, undefined)` in the uniform try/catch shape. Every caller asking whether the HUD should be visible now flows through the filter, so a handler can force the HUD hidden (a screenshot mode) or shown, regardless of the engine's own reasoning.

With zero handlers a wrap is behaviorally (not byte-) equivalent to pristine: one extra call frame and an empty-registry early-out.

## See Also

- [ui.hud_should_show](../hooks/ui.hud_should_show.md) - This is the hook this seam dispatches.
- [ui_draw_gui](ui_draw_gui.md) - Draw your own GUI instead of toggling the engine's.
- [ui_button_sprites](ui_button_sprites.md) - This is a sibling seam in `anchor_utils.gml`.
