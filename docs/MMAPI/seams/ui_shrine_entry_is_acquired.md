# Seam: ui_shrine_entry_is_acquired

Wraps the shrine menu's `entry_is_acquired()` so mods get the last word on whether an entry counts as bought.

`ui_shrine_entry_is_acquired` is a **template seam** (`op = "wrap"`). It feeds [ui.shrine_entry_is_acquired](../hooks/ui.shrine_entry_is_acquired.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/UI/Anchor/Menus/DragonshrineMenu.gml` |
| **Locator** | whole-function wrap of `entry_is_acquired()` |
| **Op** | `wrap` |
| **Feeds** | [`ui.shrine_entry_is_acquired`](../hooks/ui.shrine_entry_is_acquired.md) |
| **Value filtered** | the boolean `entry_is_acquired()` computes, which is `ARI.perks[entry.perk]` in the pristine function |
| **ctx built** | `{ perk: entry.perk }` |
| **Marker** | `mmapi_ui_shrine_entry_is_acquired` |

## The Edit

A wrap targets the whole function. The pristine `entry_is_acquired` definition inside the `DragonShrineMenu` constructor is renamed, its body untouched, and a generated wrapper takes its place right after it. The wrapper calls the renamed original and filters the computed boolean through `mmapi_apply_filters("ui.shrine_entry_is_acquired", <return>, { perk: entry.perk })` in the uniform try/catch shape. The method is a declaration nested in the constructor, the same shape as the `choose_random_artifact` wrap, so both the original and the wrapper become methods of the menu and every `self.entry_is_acquired(entry)` call resolves to the wrapper.

Every reader in the menu flows through it. The tile think callback asks once per tile per frame to pick the tint, the entry popup asks to choose between the Learn button and the Enable and Disable toggle, and the cost nodes ask to show or hide the price. The Learn button's own affordability check, `can_purchase_entry()`, is a separate method and is not wrapped.

With zero handlers a wrap is behaviorally equivalent to pristine, though not byte for byte. The only cost is one extra call frame and an early return when no handler is registered.

## See Also

- [ui.shrine_entry_is_acquired](../hooks/ui.shrine_entry_is_acquired.md) - This is the hook this seam dispatches.
- [player_perk_purchased](player_perk_purchased.md) - This is the emit in the same file that fires when the Learn button's purchase completes.
- [ui_hud_should_show](ui_hud_should_show.md) - This is the same wrap shape around another boolean predicate.
