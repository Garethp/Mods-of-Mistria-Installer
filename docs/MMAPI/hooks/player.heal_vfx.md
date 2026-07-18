# Hook: player.heal_vfx

Block the player's heal sparkle before it plays.

`player.heal_vfx` is a **guard** hook. Register a callback with `mmapi_guard`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires at the top of `play_heal_vfx()`. ctx is `{ color, sprite }`. Return `false` to veto the heal vfx (`play_heal_vfx` returns immediately and nothing is spawned). Every other return allows.

| | |
| --- | --- |
| **Fires** | At the top of `play_heal_vfx()`, before the vfx spawns. |
| **ctx** | `{ color, sprite }` |
| **Kind contract** | Only the Boolean value `false` vetoes. Every other return allows. Guards fail open: a callback that throws counts as allow. |

### The ctx struct

- `color` - the vfx color, exactly as the engine passed it to `play_heal_vfx()`.
- `sprite` - the vfx sprite, exactly as the engine passed it to `play_heal_vfx()`.

## Usage

```gml
// player.heal_vfx is a GUARD: return Boolean false to block it;
// every other return allows. Guards fail OPEN - if your handler crashes, the action happens.
function subtle_heals_player_heal_vfx(_ctx) {
    // _ctx is { color, sprite }.
    //   .color  - the vfx color play_heal_vfx received.
    //   .sprite - the vfx sprite play_heal_vfx received.
    return false; // no heal sparkle, ever - the heal itself still happens
}

// inside your latched register function (see Mod Anatomy):
mmapi_guard("player.heal_vfx", subtle_heals_player_heal_vfx);
```

## Engine Wiring

- Seam [`player_heal_vfx`](../seams/player_heal_vfx.md) dispatches from `gml/scripts/GameplaySystems/Player/Ari.gml`, at the head of `play_heal_vfx()`. On veto the engine runs `return;`.

## See Also

- [player.health_delta](player.health_delta.md) - Change the heal (or hurt) amount itself, not just its visuals.
- [audio.play_guard](audio.play_guard.md) - Suppress the accompanying sound effect the same way.
