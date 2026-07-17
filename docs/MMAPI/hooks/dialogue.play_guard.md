# Hook: dialogue.play_guard

Block a conversation before it starts.

`dialogue.play_guard` is a **guard** hook. Register a callback with `mmapi_guard`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires at the top of `play_conversation()`, before [dialogue.path](dialogue.path.md) runs. ctx is `{ npc_id, path, close_callback, args }`. Return `false` to veto the conversation (`play_conversation` returns `undefined`). `undefined` or `true` allows.

| | |
| --- | --- |
| **Fires** | At the top of `play_conversation()`, before `dialogue.path` runs. |
| **ctx** | `{ npc_id, path, close_callback, args }` |
| **Kind contract** | The callback returns `false` to veto the action. `undefined` or `true` allows it. Guards fail open: a callback that throws counts as allow. |

### The ctx struct

- `npc_id` - the NPC the conversation belongs to.
- `path` - the conversation path about to play.
- `close_callback` - the callback that would run when the conversation closes (may be `undefined`).
- `args` - the arguments passed to `play_conversation()` (may be `undefined`).

## Usage

```gml
// dialogue.play_guard is a GUARD: return false to block it, undefined (or true)
// to allow. Guards fail OPEN - if your handler crashes, the action happens.
function quiet_town_dialogue_play_guard(_ctx) {
    // _ctx is { npc_id, path, close_callback, args }.
    //   .npc_id         - the NPC the conversation belongs to.
    //   .path           - the conversation path about to play.
    //   .close_callback - would run when the conversation closes (may be undefined).
    //   .args           - the play_conversation arguments (may be undefined).
    // if (<your condition>) {
    //     return false; // veto - play_conversation returns undefined
    // }
    return undefined; // allow everything else
}

// inside your latched register function (see Mod Anatomy):
mmapi_guard("dialogue.play_guard", quiet_town_dialogue_play_guard);
```

## Engine Wiring

- Seam [`dialogue_play_guard`](../seams/dialogue_play_guard.md) dispatches from `gml/scripts/GameplaySystems/Dialogue/ConversationDriver.gml`, at the head of `play_conversation()`. On veto the engine runs `return undefined;`. It depends on [`dialogue_path`](../seams/dialogue_path.md)'s edit and lands above it, so the guard fires before the path filter.

## See Also

- [dialogue.path](dialogue.path.md) - Change which conversation plays. It has the same struct shape and runs after the guard allows.
- [dialogue.line](dialogue.line.md) - Reword any dialogue line before the textbox shows it.
- [dialogue.npc_blip](dialogue.npc_blip.md) - Swap the blip sound an NPC speaks with.
