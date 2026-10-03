# Seam: ui_spawn_tutorial_guard

Puts a veto check at the head of `spawn_tutorial()`.

`ui_spawn_tutorial_guard` is a **template seam** (`op = "guard"`). It feeds [ui.spawn_tutorial_guard](../hooks/ui.spawn_tutorial_guard.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/UI/Anchor/anchor_utils.gml` |
| **Locator** | structural target: `spawn_tutorial`, at head |
| **Op** | `guard` |
| **Feeds** | [`ui.spawn_tutorial_guard`](../hooks/ui.spawn_tutorial_guard.md) |
| **ctx built** | `{ tutorial: tutorial }` |
| **On veto** | `return undefined;` |
| **Marker** | `mmapi_ui_spawn_tutorial_guard` |

## The Edit

The generated dispatch lands at the head of `spawn_tutorial(tutorial)`. It calls `mmapi_check_guards("ui.spawn_tutorial_guard", { tutorial: tutorial })` in the uniform try/catch shape. When any guard returns `false`, the injected line runs `return undefined;` and the popup never builds. That is the same non-value the function's own test-suite early return produces, and no caller reads `spawn_tutorial`'s return, so a veto is indistinguishable from the function not having run.

The head placement has one behavioral edge worth knowing. `ARI.tutorials_seen[tutorial] = true` is the first thing the pristine body writes, and a veto happens above it, so a blocked tutorial stays unseen and its trigger will offer it again. It also means the guard fires (vacuously) in test-suite runs, above the `TEST_SUITE` bail. With zero handlers the seam is behaviorally identical to pristine.

## See Also

- [ui.spawn_tutorial_guard](../hooks/ui.spawn_tutorial_guard.md) - This is the hook this seam dispatches.
- [dialogue_play_guard](dialogue_play_guard.md) - This is the same veto shape in front of conversations.
- [ui_menu_opened](ui_menu_opened.md) - This is the emit when the anchor spawns a menu.
