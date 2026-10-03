# Hook: player.pass_out

Know when the player passes out at the end of the day.

`player.pass_out` is an **event** hook. Register a callback with `mmapi_on`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires in `pass_out()`, immediately after `end_day()`. The day rollover is already under way, stamina is zeroed, and the player is flagged invulnerable when handlers run. ctx is `{ faint }`.

`faint` is `true` for the 2 AM collapse and `false` when the player went down to a killing blow but the death was averted. `ARI.end_of_day_status` tells the cases apart. `EndOfDayStatus.Fainted` for the plain collapse, `EndOfDayStatus.Protected` otherwise. This hook is observation only. A normal bed sleep ends the day without `pass_out()`, and a death that is not averted runs the dying scene instead. See [player.died](player.died.md).

| | |
| --- | --- |
| **Fires** | In `pass_out()`, immediately after `end_day()` has run. |
| **ctx** | `{ faint }` |
| **Kind contract** | The callback observes the moment. Its return value is ignored. |

### The ctx struct

- `faint` - `true` for the 2 AM collapse, `false` for an averted death. Read `ARI.end_of_day_status` (`Fainted`, `Protected`) to tell the cases apart.

## Usage

```gml
// player.pass_out is an EVENT: the return value is ignored.
// You cannot change or stop it here; the return value is ignored.
function night_ledger_player_pass_out(_ctx) {
    // _ctx is { faint }.
    //   .faint - true for the 2 AM collapse, false for an averted death.
    // end_day() has already run: the end-of-day sequence is committed.
    // if (_ctx.faint && ARI.end_of_day_status == EndOfDayStatus.Fainted) { ... }
}

// inside your latched register function (see Mod Anatomy):
mmapi_on("player.pass_out", night_ledger_player_pass_out);
```

## Engine Wiring

- Seam [`player_pass_out`](../seams/player_pass_out.md) dispatches from `gml/scripts/Player/pass_out.gml`, immediately after the `end_day();` call.

## See Also

- [player.died](player.died.md) - This is the death path that never reaches `pass_out()`.
- [game.new_day](game.new_day.md) - Know the moment the engine's new-day logic has run, before the end-of-day autosave.
- [player.stamina_delta](player.stamina_delta.md) - Change every stamina cost or gain before it applies.
