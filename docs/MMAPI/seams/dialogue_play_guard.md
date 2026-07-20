# Seam: dialogue_play_guard

Puts a veto check at the head of `play_conversation()`.

`dialogue_play_guard` is a **template seam** (`op = "guard"`). It feeds [dialogue.play_guard](../hooks/dialogue.play_guard.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Dialogue/ConversationDriver.gml` |
| **Locator** | structural target: `play_conversation`, at head |
| **Op** | `guard` |
| **Feeds** | [`dialogue.play_guard`](../hooks/dialogue.play_guard.md) |
| **ctx built** | `{ npc_id: npc_id, path: path, close_callback: close_callback, args: args }` |
| **On veto** | `return undefined;` |
| **Depends on** | [`dialogue_path`](dialogue_path.md) |
| **Marker** | `mmapi_dialogue_run_play_guards` |

## The Edit

The generated dispatch lands at the head of `play_conversation(npc_id, path, close_callback, args)`, before anything else in the function, including the [dialogue_path](dialogue_path.md) ctx build. It calls `mmapi_check_guards("dialogue.play_guard", { npc_id, path, close_callback, args })` in the uniform try/catch shape. When any guard returns `false`, the injected line runs `return undefined;`, so the conversation never starts and no `dialogue.path` filter fires for it. At runtime the order matches the hook contracts: `dialogue.play_guard` first, `dialogue.path` after.

This entry sits near the end of the catalog on purpose. Catalog order is apply order, and the seam stacks onto a file already seamed above: `depends_on = ["dialogue_path"]` applies it after dialogue_path's head rewrite. Its anchor is pristine text that the dialogue_path replace re-emits verbatim (the `function play_conversation(...)` head, matched token-wise by the structural target) so it matches exactly once against both the pristine file and the staged file.

## See Also

- [dialogue.play_guard](../hooks/dialogue.play_guard.md) - This is the hook this seam dispatches.
- [dialogue_path](dialogue_path.md) - This is the filter this guard runs ahead of, in the same function.
- [dialogue_line](dialogue_line.md) - Reword lines of conversations you let through.
