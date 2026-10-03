# Hook: player.died

Know when the player dies.

`player.died` is an **event** hook. Register a callback with `mmapi_on`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires when the player actually dies, on `should_die()`'s final death path, after the screen fade completes and right after `MIST.run_scene("dying")` starts the dying scene. ctx is the `obj_ari` instance.

This hook is observation only. `ARI.end_of_day_status` is already `EndOfDayStatus.Died` and the scene is under way when handlers run, so handler work should be read-and-react. A revive by the Fairy status effect never gets here (health and stamina are restored and play continues). Neither does an averted death, which routes through `pass_out()` instead. See [player.pass_out](player.pass_out.md).

| | |
| --- | --- |
| **Fires** | On the final death path of `should_die()`, right after the dying scene starts. |
| **ctx** | The `obj_ari` player instance. |
| **Kind contract** | The callback observes the moment. Its return value is ignored. |

### The ctx parameter

- The `obj_ari` player instance. The death is committed, the deaths stat is counted, `ARI.end_of_day_status` is `EndOfDayStatus.Died`, the death animation has held, and the screen has faded out.

## Usage

```gml
// player.died is an EVENT: the return value is ignored.
// You cannot change or stop it here; the return value is ignored.
function memento_mori_player_died(_ctx) {
    // _ctx is the obj_ari instance.
    // The dying scene has just started and the screen is faded out.
    // React (record, tally, adjust your mod's own state). Do not fight
    // the scene from here.
}

// inside your latched register function (see Mod Anatomy):
mmapi_on("player.died", memento_mori_player_died);
```

## Engine Wiring

- Seam [`player_died`](../seams/player_died.md) dispatches from `gml/objects/characters/obj_ari.gml`, inside `should_die()`'s screen-fade callback, immediately after `MIST.run_scene("dying");`.

## See Also

- [player.pass_out](player.pass_out.md) - This event covers the 2 AM collapse and the averted death, which skip the dying scene.
- [player.incoming_damage](player.incoming_damage.md) - Change the final damage a hit deals the player, before it can kill.
- [player.health_delta](player.health_delta.md) - Change every player health gain or loss before it applies.
