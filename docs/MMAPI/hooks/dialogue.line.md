# Hook: dialogue.line

Reword any dialogue line before the textbox shows it.

`dialogue.line` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires in `ConversationDriver.deliver_line()`, before the textbox shows the line. The filtered value is the localized line text. ctx is `{ driver, line, conversation_name, npc_id, is_info_line }`. Return the replacement string, or `undefined` to keep the current value.

The filtered text is what the textbox receives however the line is delivered, whether by `say()` for speech, `info()` for info lines, or `ask()` for prompt lines.

| | |
| --- | --- |
| **Fires** | In `ConversationDriver.deliver_line()`, before the textbox shows the line. |
| **Value** | The localized line text (`line.local`). |
| **ctx** | `{ driver, line, conversation_name, npc_id, is_info_line }` |
| **Kind contract** | The callback receives the current value and returns a replacement, or `undefined` to keep the current value. |

### The ctx struct

- `driver` - the `ConversationDriver` delivering the line.
- `line` - the line struct itself. `line.local` is the unfiltered text, and `line.next_line_behavior` decides whether the line is delivered as speech, info, or a prompt.
- `conversation_name` - the driver's conversation name.
- `npc_id` - the driver's `npc_owner`, the NPC the conversation belongs to.
- `is_info_line` - `true` when the line is delivered as an info line (`textbox.info`) rather than speech.

## Usage

```gml
// dialogue.line is a FILTER: you receive (value, ctx) and return a
// replacement, or undefined to keep the game's value.
function plain_speech_dialogue_line(_value, _ctx) {
    // _value is the localized line text about to be shown.
    // _ctx is { driver, line, conversation_name, npc_id, is_info_line }.
    //   .driver            - the ConversationDriver delivering the line.
    //   .line              - the line struct; line.local is the unfiltered text.
    //   .conversation_name - the driver's conversation name.
    //   .npc_id            - the driver's npc_owner, the conversation's NPC.
    //   .is_info_line      - true when the line shows as an info line.
    if (_value == undefined) return undefined; // test undefined BEFORE anything else
    // if (_ctx.conversation_name == <a conversation you reword>) {
    //     return <your replacement text>;
    // }
    return undefined; // undefined = keep the game's text
}

mmapi_filter("dialogue.line", plain_speech_dialogue_line);
```

## Engine Wiring

- Seam [`dialogue_line`](../seams/dialogue_line.md) dispatches from `gml/scripts/GameplaySystems/Dialogue/ConversationDriver.gml`, in `deliver_line()`: it captures `line.local`, filters it, and hands the filtered text to `textbox.ask`/`info`/`say`.

## See Also

- [dialogue.speaker](dialogue.speaker.md) - Swap the speaker a textbox shows.
- [dialogue.path](dialogue.path.md) - Change which conversation plays before it starts.
- [dialogue.play_guard](dialogue.play_guard.md) - Block a conversation before it starts.
- [local.get](local.get.md) - Reword any localized text the game looks up.
