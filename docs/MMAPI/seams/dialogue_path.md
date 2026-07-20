# Seam: dialogue_path

Rebuilds `play_conversation()`'s four arguments through the `dialogue.path` filter.

`dialogue_path` is a **text seam**, a verbatim `anchor`/`replace` edit. It feeds [dialogue.path](../hooks/dialogue.path.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Dialogue/ConversationDriver.gml` |
| **Locator** | text anchor at the head of `play_conversation(npc_id, path, close_callback, args)` |
| **Feeds** | [`dialogue.path`](../hooks/dialogue.path.md) |
| **Value filtered** | the struct `{ npc_id, path, close_callback, args }` |
| **ctx built** | `undefined` |
| **Marker** | `mmapi_dialogue_run_path_filters` |

## The Edit

At the head of `play_conversation`, before the engine's opening `trace`, the replace builds `__mmapi_dialogue_ctx = { npc_id, path, close_callback, args }` from the four arguments and filters it through `mmapi_apply_filters("dialogue.path", __mmapi_dialogue_ctx, undefined)` in a try/catch. If the result is not `undefined`, each field is read back into its argument in its own per-field try/catch (`npc_id`, `path`, `close_callback`, `args`), so an `undefined` return keeps every engine value, and a partial struct keeps the engine value for exactly the fields it lacks. A handler can redirect a conversation wholesale by swapping `path`, or intercept its end by wrapping `close_callback`.

One anchoring subtlety, from the catalog itself: [dialogue_play_guard](dialogue_play_guard.md) stacks onto this same function with `depends_on = ["dialogue_path"]`, and is appended after the earlier entries on purpose. Catalog order is apply order. Its head-of-function locator is pristine text that this seam's replace re-emits verbatim (the `function play_conversation(...)` line), so it matches exactly once against both the pristine file and the staged file. The guard's dispatch lands above this seam's ctx build, so at runtime `dialogue.play_guard` runs first, then `dialogue.path`, exactly the order the hook contracts state.

## See Also

- [dialogue.path](../hooks/dialogue.path.md) - This is the hook this seam dispatches.
- [dialogue_play_guard](dialogue_play_guard.md) - This seam is the veto that runs just above this filter.
- [dialogue_line](dialogue_line.md) - This seam filters individual lines instead of the whole conversation.
- [dialogue_speaker_ctx_arg](dialogue_speaker_ctx_arg.md) - This seam is the same file's companion edit for `dialogue.speaker`.
