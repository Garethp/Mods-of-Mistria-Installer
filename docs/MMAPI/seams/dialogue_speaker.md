# Seam: dialogue_speaker

Filters the just-built textbox speaker before it is assigned.

`dialogue_speaker` is a **text seam**, a verbatim `anchor`/`replace` edit. It feeds [dialogue.speaker](../hooks/dialogue.speaker.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/T2r.gml` |
| **Locator** | text anchor in the T2 Speaker action handler, between the speaker construction and `textbox.requested_speaker = speaker;` |
| **Feeds** | [`dialogue.speaker`](../hooks/dialogue.speaker.md) |
| **Value filtered** | the `NpcSpeaker` or `CameoSpeaker` just built from `action.speaker` |
| **ctx built** | `{ action: action, current_npc_id: current_npc_id, driver: ctx, conversation_name: <read defensively off ctx>, textbox: textbox, is_cameo: is_cameo }` |
| **Marker** | `mmapi_dialogue_run_speaker_filters` |

## The Edit

The T2 Speaker handler builds the speaker (`new CameoSpeaker(...)` when `action.speaker` names a cameo, else `new NpcSpeaker(...)`) and assigns it to `textbox.requested_speaker`. The replace inserts the dispatch between the two. It first reads `conversation_name` defensively off the threaded `ctx` in its own try/catch (`ctx.conversation_name`): `ctx` is the ConversationDriver when one was threaded through `process_t2_action`, and when it wasn't the failed read simply leaves the name `undefined`. It then filters `speaker` through `mmapi_apply_filters("dialogue.speaker", speaker, { ... })` with the action, the current npc id, the driver, the conversation name, the textbox, and the `is_cameo` flag. Whatever the filter chain returns is what lands in `textbox.requested_speaker`.

The engine threads the driver for mid-conversation Speaker actions. The companion seam [dialogue_speaker_ctx_arg](dialogue_speaker_ctx_arg.md), which dispatches nothing itself, threads it for the conversation's initial speaker too, so `driver` and `conversation_name` are filled whenever a conversation is playing.

## See Also

- [dialogue.speaker](../hooks/dialogue.speaker.md) - This is the hook this seam dispatches.
- [dialogue_speaker_ctx_arg](dialogue_speaker_ctx_arg.md) - This is the companion edit that fills this seam's ctx from a conversation's first line.
- [dialogue_npc_blip](dialogue_npc_blip.md) - Filter the speaker's blip sound inside the `NpcSpeaker` constructor.
- [dialogue_line](dialogue_line.md) - Filter the line text the speaker delivers.
