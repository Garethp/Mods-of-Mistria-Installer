# Hook: dialogue.path

Change which conversation plays before it starts.

`dialogue.path` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires at the top of `play_conversation()`, after [dialogue.play_guard](dialogue.play_guard.md). The filtered value is the struct `{ npc_id, path, close_callback, args }`. ctx is `undefined`. Return the replacement struct. The seam tolerates an `undefined` return and re-reads every field defensively, so `undefined` (or a partial struct) keeps the engine values.

| | |
| --- | --- |
| **Fires** | At the top of `play_conversation()`, after `dialogue.play_guard`. |
| **Value** | The struct `{ npc_id, path, close_callback, args }` - `play_conversation()`'s arguments. |
| **ctx** | `undefined` |
| **Kind contract** | The callback receives the current value and returns a replacement, or `undefined` to keep the current value. |

### The value struct

- `npc_id` - the NPC the conversation belongs to.
- `path` - the conversation path about to play.
- `close_callback` - the callback run when the conversation closes (may be `undefined`).
- `args` - the arguments passed to `play_conversation()` (may be `undefined`).

Every field is re-read defensively after the filter chain: return the full struct, a partial struct, or `undefined`. Any field you leave out keeps its engine value.

## Usage

```gml
// dialogue.path is a FILTER: you receive (value, ctx) and return a
// replacement, or undefined to keep the game's value.
function understudy_dialogue_path(_value, _ctx) {
    // _value is the conversation request { npc_id, path, close_callback, args }.
    //   .npc_id         - the NPC the conversation belongs to.
    //   .path           - the conversation path about to play.
    //   .close_callback - run when the conversation closes (may be undefined).
    //   .args           - the play_conversation arguments (may be undefined).
    // _ctx is undefined.
    if (_value == undefined) return undefined; // test undefined BEFORE anything else
    // redirect a conversation you own:
    // if (_value.path == <the engine path>) {
    //     _value.path = <your replacement path>;
    //     return _value;
    // }
    return undefined; // undefined (or a partial struct) = keep the engine values
}

mmapi_filter("dialogue.path", understudy_dialogue_path);
```

## Engine Wiring

- Seam [`dialogue_path`](../seams/dialogue_path.md) dispatches from `gml/scripts/GameplaySystems/Dialogue/ConversationDriver.gml`, at the top of `play_conversation()`: it builds the request struct, filters it, and re-reads `npc_id`, `path`, `close_callback`, and `args` defensively before the conversation starts. The [`dialogue_play_guard`](../seams/dialogue_play_guard.md) seam depends on this one and lands above it, so the guard fires first.

## See Also

- [dialogue.play_guard](dialogue.play_guard.md) - Block a conversation before it starts (fires first, same struct shape).
- [dialogue.line](dialogue.line.md) - Reword any dialogue line before the textbox shows it.
- [dialogue.speaker](dialogue.speaker.md) - Swap the speaker a textbox shows.
