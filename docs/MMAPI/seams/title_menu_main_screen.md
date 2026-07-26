# Seam: title_menu_main_screen

Emits the title-screen entry from the head of `TitleMenu.setup_main_screen()`, at boot and on quit-to-title.

`title_menu_main_screen` is a **template seam** (`op = "emit"`). It feeds [game.title_entered](../hooks/game.title_entered.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/UI/Anchor/Menus/TitleMenu.gml` |
| **Locator** | structural target: `setup_main_screen`, at head |
| **Op** | `emit` |
| **Feeds** | [`game.title_entered`](../hooks/game.title_entered.md) |
| **ctx built** | `{ from_game: FROM_GAME }` |
| **Marker** | `mmapi_game_run_title_entered` |

## The Edit

The generated dispatch lands at the head of `TitleMenu.setup_main_screen()`, the one method both title entries funnel through. At boot, Setup spawns the title menu and its start chain reaches the main screen after the logo screens. On quit-to-title, a fresh Setup runs its `FROM_GAME` branch and the start chain skips the logos and goes straight there. It calls `mmapi_emit("game.title_entered", { from_game: FROM_GAME })` in the uniform try/catch shape; `FROM_GAME` is still set on the quit path at this point (it only resets inside `enter_game`), so the ctx tells handlers which entry this is. With zero handlers the seam is behaviorally identical to pristine: the emit early-outs on an empty registry.

This anchor is the same method the legacy YYTK mods hooked (`gml_Script_setup_main_screen@TitleMenu@TitleMenu`), chosen for the same reason: it is the engine's own "the title screen is up" moment. An in-file seam is the only way to observe it — the derived-events poll runs from the Game object's begin_step, and no Game instance ever steps in the title room.

## See Also

- [game.title_entered](../hooks/game.title_entered.md) - This is the hook this seam dispatches.
- [save_game_loaded](save_game_loaded.md) - This is the matching start-of-session seam. It announces the save load that ends a stay on the title screen.
