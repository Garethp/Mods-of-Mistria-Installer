# Hook: player.renown_delta

Change every renown gain before it applies, or turn it into a deduction.

`player.renown_delta` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires at the top of `Ari.modify_renown()`, before the delta is applied. The filtered value is the renown delta. The ctx is `{ player }`. Return the replacement delta (`0` makes the call a no-op), or `undefined` to keep the current value.

Renown gains queue as pending entries during the day - quest completions, museum donations, the day's shipping gold - and drain at day rollover, one `modify_renown` call per entry. That drain is the engine's only gameplay caller, so this hook fires once per pending entry as the new day starts. Absolute sets (debug, test suite, new game) do not route through `modify_renown` and never fire this hook.

`set_renown` clamps the resulting total to `[0, max renown]`, so any replacement is safe. A negative replacement lowers renown and the computed level, plays no celebration, and never revokes already-granted level rewards. A gain that crosses several levels grants each crossed level's reward.

> [!TIP]
> The end-of-day menu tallies the pending entries before the drain, so its renown-earned figure shows the unfiltered values. The filter applies afterward, as the entries land.

| | |
| --- | --- |
| **Fires** | At the top of `Ari.modify_renown(amount_to_add)`, before the delta is applied. |
| **Value** | The renown delta. Vanilla deltas are always gains; a negative replacement deducts. |
| **ctx** | `{ player }` |
| **Kind contract** | The callback receives the current value and returns a replacement, or `undefined` to keep the current value. |

### The ctx struct

- `player` - the `Ari` struct whose renown is changing.

## Usage

```gml
// player.renown_delta is a FILTER: you receive (value, ctx) and return a
// replacement, or undefined to keep the game's value.
function town_favorite_player_renown_delta(_value, _ctx) {
    // _value is the renown delta: one pending entry's worth, drained at day
    // rollover. Negative deducts; set_renown clamps the total at zero.
    // _ctx is { player }.
    //   .player - the Ari struct whose renown is changing.
    if (_value == undefined) return undefined; // test undefined BEFORE anything else
    if (_value > 0) return _value * 2; // double every renown gain
    return undefined; // undefined = keep the game's value
}

mmapi_filter("player.renown_delta", town_favorite_player_renown_delta);
```

## Engine Wiring

- Seam [`player_renown_delta`](../seams/player_renown_delta.md) dispatches from `gml/scripts/GameplaySystems/Player/Ari.gml`, at the head of `modify_renown(amount_to_add)`, filtering `amount_to_add` before the engine's `set_renown` clamps and applies the total.

## See Also

- [player.xp_delta](player.xp_delta.md) - The similar filter point for skill XP.
- [player.gold_delta](player.gold_delta.md) - The filter point for the shipping gold that becomes a renown entry.
- [npc.heart_points](npc.heart_points.md) - The equivalent delta filter for villager hearts.
