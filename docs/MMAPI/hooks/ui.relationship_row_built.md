# Hook: ui.relationship_row_built

Add custom nodes to each NPC row in the relationships journal.

`ui.relationship_row_built` is an **event** hook. Register a callback with `mmapi_on`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires once per NPC row as the relationships journal page builds its scrollable list, at the end of the row build with every vanilla node in place. Each journal tab switch constructs the page fresh, so the hook fires for every listed NPC on every visit to the page and never per frame. The test suite's menu stress builds this page too, so handlers also fire there.

ctx is `{ menu, element, npc_id, npc, portrait, hearts, bar, talk_icon, talk_check, gift_icon, gift_check }`. `menu` is the `RelationshipsMenu` under construction, `element` is the row's scroller element (its name label is `element.text_label`), and `npc` is the `Npc` data struct for `npc_id`. The rest are the row's anchor nodes. `portrait` is the NPC sprite, `hearts` and `bar` are the heart level and heart progress sprites, `talk_icon` and `talk_check` are the talked-today icon and checkbox, and `gift_icon` and `gift_check` are the gifted-today icon and checkbox.

| | |
| --- | --- |
| **Fires** | At the end of each row build in the `RelationshipsMenu` constructor, once per listed NPC per page build. |
| **ctx** | The row struct above. Anchor new nodes to the ctx nodes. |
| **Kind contract** | The callback observes the moment. Its return value is ignored. Adding nodes to the row is the intended use. |

Nodes parented to the row are freed with the menu, so handlers need no cleanup. Row content is a build-time snapshot, the vanilla checkboxes included, so give a node a think callback if it must track live state while the page is open.

Unmet NPCs get rows too, with a `???` label, a blacked-out portrait, and no tap callback, so gate revealing decorations on `ctx.npc.has_met()`. NPCs the list skips never fire. That covers locked NPCs, unmet vendors, and unmet animal-tagged NPCs before `greet_the_townsfolk` completes. The right-page detail pane (`set_to_npc_id`) is separate and not covered by this hook.

## Usage

```gml
// ui.relationship_row_built is an EVENT: the return value is ignored.
// Fires once per listed NPC each time the relationships page is built.
function my_mod_relationship_row(_ctx) {
    // Unmet NPCs get "???" rows: add nothing that reveals them.
    if (!_ctx.npc.has_met()) {
        return;
    }

    // A second gift checkbox, chained off the vanilla one exactly the way
    // the vanilla row chains talk_check -> gift_icon -> its own checkbox.
    // my_mod_extra_gift_given() is your own state: the engine's gift_flag
    // only tracks the first gift of the day.
    ANCHOR.sprite(_ctx.gift_check)
        .set_sprite(my_mod_extra_gift_given(_ctx.npc_id)
            ? spr_ui_generic_checkbox_on
            : spr_ui_generic_checkbox_off)
        .set_align(Align.RightOut, Align.Middle)
        .set_x(2)
}

// inside your latched register function (see Mod Anatomy):
mmapi_on("ui.relationship_row_built", my_mod_relationship_row);
```

Handlers run once per row at menu-build frequency, so ordinary lookups are fine, but keep heavy work out of the loop over thirty-plus rows.

## Interactions

- The rows emit from inside the menu constructor, before `spawn_menu` pushes the menu onto `open_menus`, so every row emit for a build lands before that build's [ui.menu_opened](ui.menu_opened.md). During a row emit `ANCHOR.get_menu(Menu.Relationships)` can still return the previous, closing instance from the tab switch that triggered this build. Use `ctx.menu`, never a lookup.
- Text nodes added with `set_key` resolve through the localizer, so a label's key flows through [local.get](local.get.md) and, when the key does not ship as data, [local.missing](local.missing.md) can serve it at runtime.

## Engine Wiring

- Seam [`ui_relationship_row_built`](../seams/ui_relationship_row_built.md) dispatches from `gml/scripts/UI/Anchor/Menus/RelationshipsMenu.gml`, at the end of the constructor's per-NPC row loop.

## See Also

- [ui.menu_opened](ui.menu_opened.md) - The whole-menu open moment, after every row has built.
- [ui.menu_refreshed](ui.menu_refreshed.md) - Rebuild edges for the menus that rebuild in place.
- [npc.gift_received](npc.gift_received.md) - The gift moment itself, for keeping the state a row decoration shows.
- [local.missing](local.missing.md) - Serve a label's localization key at runtime.
