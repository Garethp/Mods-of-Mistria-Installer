# Hook: dialogue.npc_blip

Swap the blip sound an NPC speaks with.

`dialogue.npc_blip` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires in the `NpcSpeaker` constructor, after the default blip noise lookup. The filtered value is the blip sound. ctx is `{ npc_id, speaker }`. Return the replacement sound, or `undefined` to keep the current value.

| | |
| --- | --- |
| **Fires** | In the `NpcSpeaker` constructor, after the default blip noise lookup. |
| **Value** | The blip sound, as `find_npc_blip_noise()` chose it. |
| **ctx** | `{ npc_id, speaker }` |
| **Kind contract** | The callback receives the current value and returns a replacement, or `undefined` to keep the current value. |

### The ctx struct

- `npc_id` - the NPC's id (the speaker's `self.id`).
- `speaker` - the `NpcSpeaker` under construction, whose `me`, `identity`, and default `blip_sound` are already set.

## Usage

```gml
// dialogue.npc_blip is a FILTER: you receive (value, ctx) and return a
// replacement, or undefined to keep the game's value.
function voice_tuner_dialogue_npc_blip(_value, _ctx) {
    // _value is the blip sound the default lookup chose.
    // _ctx is { npc_id, speaker }.
    //   .npc_id  - the NPC's id.
    //   .speaker - the NpcSpeaker under construction (me, identity, and the
    //              default blip_sound are already set).
    if (_value == undefined) return undefined; // test undefined BEFORE anything else
    // if (_ctx.npc_id == <the NPC you retune>) return <your blip sound>;
    return undefined; // undefined = keep the game's value
}

mmapi_filter("dialogue.npc_blip", voice_tuner_dialogue_npc_blip);
```

## Engine Wiring

- Seam [`dialogue_npc_blip`](../seams/dialogue_npc_blip.md) dispatches from `gml/scripts/UI/Anchor/Menus/TextboxMenu.gml`, in the `NpcSpeaker` constructor, filtering `self.blip_sound` right after `find_npc_blip_noise(self.id)` assigns it.

## See Also

- [dialogue.speaker](dialogue.speaker.md) - Swap the speaker a textbox shows.
- [dialogue.line](dialogue.line.md) - Reword any dialogue line before the textbox shows it.
- [audio.play_guard](audio.play_guard.md) - Block any sound effect before it plays.
