# Seam: dialogue_conversation_finished

Emits at the end of `finish_conversation()`, after the end actions, the textbox close, and the state write.

`dialogue_conversation_finished` is a **template seam** (`op = "emit"`). It feeds [dialogue.conversation_finished](../hooks/dialogue.conversation_finished.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Dialogue/ConversationDriver.gml` |
| **Locator** | structural target: `finish_conversation`, after `self.state = ConversationDriverState.Finished;` |
| **Op** | `emit` |
| **Feeds** | [`dialogue.conversation_finished`](../hooks/dialogue.conversation_finished.md) |
| **ctx built** | `{ driver: self, conversation_name: self.conversation_name, npc_id: self.npc_owner, end_actions: end_actions }` |
| **Marker** | `mmapi_dialogue_run_conversation_finished` |

## The Edit

`finish_conversation()` is a method of the `ConversationDriver` constructor. It asks the T2 runtime to end the conversation with `T2R.conversation_end()`, runs each returned action through `process_t2_action()`, closes the textbox, and writes `ConversationDriverState.Finished`. The state write is its last statement, and the generated emit lands right after it, so the emit runs once everything the method does has run. The dispatch is `mmapi_emit("dialogue.conversation_finished", { ... })` in the uniform try/catch shape, one ctx field per line, and the `end_actions` field reads the method's own local, which is still in scope there.

The textbox close takes one of two branches. A textbox in the `Hidden` or `Translating` state is closed with `close()` at once. Any other textbox gets `begin_close()`, which plays the closing animation and then runs the textbox's close callback, the one `ensure_textbox()` registered to run the driver's `close_callback`. Both branches finish before the emit. The textbox stays in `ANCHOR.open_menus` on both branches until the Anchor's begin step frees it, so the emit precedes the textbox's `ui.menu_closed` by at least a frame.

Every driver that finishes passes through this method. `proceed_conversation()` calls it when the current line's next line behavior is `Finish`, or when the prompt the player picked finished the conversation. The cutscene runtime calls it from `Mist.clean_up_scene()` (directly on a skip, and through `proceed_conversation()` otherwise) and from the `set_conversation` script function, which finishes the scene's current driver before it builds the next. With zero handlers the seam is behaviorally identical to pristine.

## Edge Cases

- A skipped cutscene closes the textbox with `close(true)` and sets `driver.textbox` to `undefined` before it calls `finish_conversation()`, so the textbox branch is skipped and the emit still fires.
- The engine's test suite ends a conversation by closing the textbox and calling `T2R.conversation_end()` itself, without the driver, so that path never reaches this seam.

## See Also

- [dialogue.conversation_finished](../hooks/dialogue.conversation_finished.md) - This is the hook this seam dispatches.
- [dialogue_play_guard](dialogue_play_guard.md) - This is the veto at the other end of the conversation, at the head of `play_conversation()` in this same file.
- [dialogue_line](dialogue_line.md) - This is the filter on every line the driver delivers, in this same file.
- [ui_menu_closed_drain](ui_menu_closed_drain.md) - This is the emit that fires when the finished textbox leaves the open menus.
