# Seam: ui_relationship_row_built

Hands each finished NPC row to mods as the relationships journal builds its list.

`ui_relationship_row_built` is a **text seam**. It feeds [ui.relationship_row_built](../hooks/ui.relationship_row_built.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/UI/Anchor/Menus/RelationshipsMenu.gml` |
| **Locator** | text anchor on the row loop's trailing gift checkbox chain |
| **Feeds** | [`ui.relationship_row_built`](../hooks/ui.relationship_row_built.md) |
| **ctx built** | `{ menu, element, npc_id, npc, portrait, hearts, bar, talk_icon, talk_check, gift_icon, gift_check }` |
| **Marker** | `mmapi_ui_relationship_row_built` |

## The Edit

The `RelationshipsMenu` constructor builds the scrollable NPC list in one loop, one scroller element per listed NPC, and the loop body's last statement is the gifted-today checkbox chain. The pristine chain discards its node, so the template ops cannot put it in a ctx. The text seam prepends `var gift_check = ` to the chain, then emits `mmapi_emit("ui.relationship_row_built", { ... })` in the uniform try/catch shape with the row's locals packed as the ctx.

The emit is the new last statement of the row build, so every vanilla node exists when handlers run. The loop's visibility gates (`npc_is_unlocked`, the unmet-vendor skip, and the unmet-animal skip before `greet_the_townsfolk`) all `continue` above the emit, so skipped NPCs never dispatch. The constructor runs on every journal tab switch into the relationships page, because `JournalMenu.set_active_sub_menu` frees the old sub-menu and spawns a fresh one, and `self` at the emit is the menu still under construction, not yet pushed onto `open_menus`.

## See Also

- [ui.relationship_row_built](../hooks/ui.relationship_row_built.md) - This is the hook this seam dispatches.
- [ui_menu_opened](ui_menu_opened.md) - The whole-menu emit that follows every row emit of a build.
