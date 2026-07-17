# Seam: dialogue_speaker_ctx_arg

Threads the ConversationDriver into the initial Speaker action so `dialogue.speaker`'s ctx is filled from line one.

`dialogue_speaker_ctx_arg` is a **text seam** and a **companion edit**: it dispatches nothing itself. It exists for [dialogue.speaker](../hooks/dialogue.speaker.md), whose dispatch lives in [dialogue_speaker](dialogue_speaker.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Dialogue/ConversationDriver.gml` |
| **Locator** | text anchor on the driver's initial-speaker call to `process_t2_action(...)` |
| **Feeds** | [`dialogue.speaker`](../hooks/dialogue.speaker.md) (no dispatch of its own) |
| **Marker** | `mmapi_dialogue_initial_speaker_ctx` |

## The Edit

This seam dispatches nothing. It is a one-argument thread. Where the ConversationDriver sets up the conversation's initial speaker, `process_t2_action(T2Action.Speaker(npc_id_to_string(self.npc_owner)), self.npc_owner)`, the replace appends `self`, the driver, as a third argument.

The engine already threads the driver through `process_t2_action` for mid-conversation Speaker actions. This edit covers the initial one. When [dialogue_speaker](dialogue_speaker.md) dispatches in the T2 Speaker handler, its ctx's `driver` and `conversation_name` fields are therefore filled from the very start of a conversation, not just after the first mid-conversation speaker change.

## See Also

- [dialogue.speaker](../hooks/dialogue.speaker.md) - This is the hook this companion edit serves.
- [dialogue_speaker](dialogue_speaker.md) - This is the dispatching seam whose ctx this thread fills.
- [dialogue_path](dialogue_path.md) - This is a sibling seam in `ConversationDriver.gml`.
