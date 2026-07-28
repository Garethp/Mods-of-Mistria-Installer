# Seam: setup_title_entry

Emits the title-screen entry from Setup's create, right after the title menu is spawned, at boot and on quit-to-title.

`setup_title_entry` is a **text seam** (`anchor` + `replace`). It feeds [game.title_entered](../hooks/game.title_entered.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/objects/Setup.gml` |
| **Locator** | text anchor on the title-menu spawn pair in Setup's create (`ANCHOR.spawn_menu(Menu.Title)` + `menu.start()`) |
| **Op** | text (`anchor` + `replace`) |
| **Feeds** | [`game.title_entered`](../hooks/game.title_entered.md) |
| **ctx built** | `{ from_game: FROM_GAME }` |
| **Marker** | `mmapi_game_run_title_entered` |

## The Edit

The replacement re-emits the spawn pair verbatim and inserts the dispatch directly after `menu.start()`: `mmapi_emit("game.title_entered", { from_game: FROM_GAME })` in the uniform try/catch shape. Setup's create runs exactly once per title entry — at boot, and again on quit-to-title when the engine builds a fresh Setup — so the emit fires once per entry with no dependence on the title menu's internal flow. With zero handlers the seam is behaviorally identical to pristine: the emit early-outs on an empty registry.

The anchor point is what makes the `from_game` discriminator work: `FROM_GAME` is cleared at the end of Setup's create, and the title menu's own start chain runs frames later, so the spawn pair is the one moment that fires once per entry while the flag still distinguishes the two — `true` on the quit path, `false` at boot. An in-file seam is the only way to observe this moment at all: the derived-events poll runs from the Game object's begin_step, and no Game instance ever steps in the title room.

Handlers therefore run during Setup's create, before the title menu is drawn or interactive.

## See Also

- [game.title_entered](../hooks/game.title_entered.md) - This is the hook this seam dispatches.
- [save_game_loaded](save_game_loaded.md) - This is the matching start-of-session seam. It announces the save load that ends a stay on the title screen.
