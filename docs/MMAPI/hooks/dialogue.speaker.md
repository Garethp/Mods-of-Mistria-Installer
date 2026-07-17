# Hook: dialogue.speaker

Swap the speaker a textbox shows.

`dialogue.speaker` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires in the T2 `Speaker` action handler when a textbox speaker is built. The filtered value is the `NpcSpeaker` or `CameoSpeaker` about to be assigned. ctx is `{ action, current_npc_id, driver, conversation_name, textbox, is_cameo }`.

The engine threads the `ConversationDriver` through for mid-conversation actions, and a companion seam (which dispatches nothing itself) threads it for the conversation's initial speaker too, so `driver` and `conversation_name` are filled whenever a conversation is playing. Return the replacement speaker, or `undefined` to keep the current value.

| | |
| --- | --- |
| **Fires** | In the T2 `Speaker` action handler, when a textbox speaker is built. |
| **Value** | The `NpcSpeaker` or `CameoSpeaker` about to be assigned to `textbox.requested_speaker`. |
| **ctx** | `{ action, current_npc_id, driver, conversation_name, textbox, is_cameo }` |
| **Kind contract** | The callback receives the current value and returns a replacement, or `undefined` to keep the current value. |

### The ctx struct

- `action` - The T2 `Speaker` action being processed. `action.speaker` is the speaker string the engine resolved into the value.
- `current_npc_id` - the npc id the action handler received alongside the action (the conversation's `npc_owner` for the initial speaker).
- `driver` - the `ConversationDriver` threaded through: by the engine for mid-conversation `Speaker` actions, and by the companion seam for the conversation's initial speaker, filled whenever a conversation is playing.
- `conversation_name` - the driver's conversation name, read defensively (`undefined` when no driver is available).
- `textbox` - the textbox whose `requested_speaker` the value is about to be assigned to.
- `is_cameo` - `true` when the speaker string resolved to a cameo id and the value is a `CameoSpeaker`, and `false` for an `NpcSpeaker`.

## Usage

```gml
// dialogue.speaker is a FILTER: you receive (value, ctx) and return a
// replacement, or undefined to keep the game's value.
function puppet_master_dialogue_speaker(_value, _ctx) {
    // _value is the NpcSpeaker or CameoSpeaker about to be assigned.
    // _ctx is { action, current_npc_id, driver, conversation_name, textbox, is_cameo }.
    //   .action            - the T2 Speaker action; action.speaker is the speaker string.
    //   .current_npc_id    - the npc id the action handler received.
    //   .driver            - the ConversationDriver (filled whenever a conversation is playing).
    //   .conversation_name - the driver's conversation name (undefined without a driver).
    //   .textbox           - the textbox the speaker is being assigned to.
    //   .is_cameo          - true when _value is a CameoSpeaker.
    if (_value == undefined) return undefined; // test undefined BEFORE anything else
    // if (_ctx.conversation_name == <a conversation you own>) {
    //     return new NpcSpeaker(<your npc id>);
    // }
    return undefined; // undefined = keep the game's speaker
}

mmapi_filter("dialogue.speaker", puppet_master_dialogue_speaker);
```

## Engine Wiring

- Seam [`dialogue_speaker`](../seams/dialogue_speaker.md) dispatches from `gml/scripts/GameplaySystems/T2r.gml`, in the `Speaker` action handler, filtering the built speaker before `textbox.requested_speaker` is assigned.
- Companion seam [`dialogue_speaker_ctx_arg`](../seams/dialogue_speaker_ctx_arg.md) provides no dispatch of its own: it threads the `ConversationDriver` into `process_t2_action` for the conversation's initial speaker, so `ctx.driver` and `ctx.conversation_name` are filled there too.

## See Also

- [dialogue.npc_blip](dialogue.npc_blip.md) - Swap the blip sound an NPC speaks with.
- [dialogue.line](dialogue.line.md) - Reword any dialogue line before the textbox shows it.
- [dialogue.path](dialogue.path.md) - Change which conversation plays before it starts.
