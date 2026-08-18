# Hook: ui.spawn_tutorial_guard

Block a tutorial popup before it spawns.

`ui.spawn_tutorial_guard` is a **guard** hook. Register a callback with `mmapi_guard`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires at the top of `spawn_tutorial(tutorial)`, before the tutorial is marked seen and before its popup is built. ctx is `{ tutorial }`, the `Tutorial` enum id. Return `false` to veto the popup (`spawn_tutorial` returns `undefined`). Every other return allows.

A vetoed tutorial stays unmarked in `ARI.tutorials_seen`, so the game offers it again on its next trigger. A handler that wants a tutorial gone for good must veto every time (cheap), or mark it seen itself. The guard sits above the function's own test-suite early return, so it also fires, vacuously, in test-suite runs.

| | |
| --- | --- |
| **Fires** | At the top of `spawn_tutorial(tutorial)`, before the seen-flag write and the popup build. |
| **ctx** | `{ tutorial }` |
| **Kind contract** | Only the Boolean value `false` vetoes. Every other return allows. Guards fail open: a callback that throws counts as allow. |

### The ctx struct

- `tutorial` - the `Tutorial` enum id about to be shown.

## Usage

```gml
// ui.spawn_tutorial_guard is a GUARD: return Boolean false to block it;
// every other return allows. Guards fail OPEN - if your handler crashes, the action happens.
function seasoned_farmer_ui_spawn_tutorial_guard(_ctx) {
    // _ctx is { tutorial }.
    //   .tutorial - the Tutorial enum id about to be shown.
    // A vetoed tutorial is NOT marked seen, so it will try again on its
    // next trigger. Keep vetoing (or set ARI.tutorials_seen yourself).
    // if (<your condition>) {
    //     return false; // veto - spawn_tutorial returns undefined
    // }
    return undefined; // allow everything else
}

// inside your latched register function (see Mod Anatomy):
mmapi_guard("ui.spawn_tutorial_guard", seasoned_farmer_ui_spawn_tutorial_guard);
```

## Engine Wiring

- Seam [`ui_spawn_tutorial_guard`](../seams/ui_spawn_tutorial_guard.md) dispatches from `gml/scripts/UI/Anchor/anchor_utils.gml`, at the head of `spawn_tutorial()`. On veto the engine runs `return undefined;`.

## See Also

- [ui.menu_opened](ui.menu_opened.md) - Know the moment a menu opens.
- [dialogue.play_guard](dialogue.play_guard.md) - Block a conversation before it starts, the same veto shape.
