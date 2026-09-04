# Hook: dialogue.romance_prompt_guard

Grey and lock a pink romance prompt for your own reasons.

`dialogue.romance_prompt_guard` is a **guard** hook. Register a callback with `mmapi_guard`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires while a pink romance prompt is styled for display, after the vanilla married or engaged lock is evaluated and only when that lock leaves the prompt selectable. ctx is `{ npc_id, speaker, path }`. Return `false` to grey and soft-lock the prompt exactly as the vanilla lock does. Every other return keeps it selectable.

Pink prompts are marked by the mist command `make_next_prompt_pink`, so this hook fires only during MIST cutscenes. The guard adds lock reasons on top of the vanilla lock and cannot remove it. When the player is married or engaged and break-ups are enabled, the prompt is locked before the guard is consulted and no dispatch happens.

| | |
| --- | --- |
| **Fires** | While a pink prompt box is styled, once per pink prompt box per styling pass, in `TextboxMenu`'s prompt `reset_sprites`. |
| **ctx** | `{ npc_id, speaker, path }` |
| **Kind contract** | Only the Boolean value `false` vetoes. Every other return allows. Guards fail open: a callback that throws counts as allow. |

### The ctx struct

- `npc_id` - the current textbox speaker's `NpcId` (undefined for a cameo speaker or when no speaker is set).
- `speaker` - the live `Speaker` struct, or undefined.
- `path` - the t2 conversation name the textbox is playing, or undefined.

## Usage

```gml
// dialogue.romance_prompt_guard is a GUARD: return Boolean false to lock the
// prompt; every other return keeps it selectable. Guards fail OPEN - if your
// handler crashes, the prompt stays selectable.
function my_mod_romance_prompt_guard(_ctx) {
    // _ctx is { npc_id, speaker, path }.
    //   .npc_id  - the textbox speaker's NpcId (may be undefined).
    //   .speaker - the live Speaker struct (may be undefined).
    //   .path    - the t2 conversation name (may be undefined).
    if (_ctx.npc_id == NpcId.Balor && T2R.read("my_mod_balor_romance_locked") == true) {
        return false; // grey and soft-lock the prompt
    }
    return undefined; // allow everything else
}

// inside your latched register function (see Mod Anatomy):
mmapi_guard("dialogue.romance_prompt_guard", my_mod_romance_prompt_guard);
```

A handler decides from state it owns, typically a t2 world fact the mod's own content writes with `write_world_fact`. Multiple mods compose naturally, because any single `false` locks the prompt.

## Interactions

- The styling pass runs twice per pink ask, once as the prompts slide in and once more while the prompt row is cleaned up after a selection. A veto during the cleanup pass re-inserts the engine's `stay_locked` flag, which the next prompt round in the same conversation consumes. The vanilla married lock behaves identically. Answer from stable state rather than per-call logic so both passes agree.

## Engine Wiring

- Seam [`dialogue_romance_prompt_guard`](../seams/dialogue_romance_prompt_guard.md) dispatches from `gml/scripts/UI/Anchor/Menus/TextboxMenu.gml`, inside the prompt boxes' `reset_sprites` closure. On veto the engine applies the same grey sprites and soft lock as the vanilla married or engaged condition.

## See Also

- [dialogue.play_guard](dialogue.play_guard.md) - Block a whole conversation before it starts.
- [dialogue.line](dialogue.line.md) - Reword any dialogue line before the textbox shows it.
- [dialogue.speaker](dialogue.speaker.md) - Swap the speaker a textbox shows.
- [date.begin](date.begin.md) - Cancel an accepted date before its cutscene.
