# Hook: renown.rank_gained

Know the moment the player reaches a new renown rank.

`renown.rank_gained` is an **event** hook. Register a callback with `mmapi_on`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires inside `Ari.set_renown()` when a renown level gain crosses a rank boundary, immediately after [renown.level_gained](renown.level_gained.md). ctx is `{ old_rank, new_rank }`, each as the integer indexes into `RENOWN.ranks`, so `RENOWN.ranks.get(new_rank)` is the rank struct with its name and sprites.

A gain within one rank fires `renown.level_gained` alone, and once the final rank is reached, further level gains never fire this hook again. The same caller notes apply as for its sibling. The gameplay path is the day-rollover drain, debug sets also route through, and save load never fires.

| | |
| --- | --- |
| **Fires** | Inside `Ari.set_renown()`, when a level gain crosses a rank boundary, right after `renown.level_gained`. |
| **ctx** | `{ old_rank, new_rank }` |
| **Kind contract** | The callback observes the moment. Its return value is ignored. |

### The ctx struct

- `old_rank` - the rank index before the gain.
- `new_rank` - the rank index after, clamped to the last rank. `RENOWN.ranks.get(new_rank)` resolves the struct.

## Usage

```gml
// renown.rank_gained is an EVENT: the return value is ignored.
// You cannot change or stop it here; the return value is ignored.
function rank_banner_renown_rank_gained(_ctx) {
    // _ctx is { old_rank, new_rank }.
    //   .old_rank - the rank index before the gain.
    //   .new_rank - the rank index after, clamped to the last rank.
    // RENOWN.ranks.get(_ctx.new_rank) is the rank struct (name, sprites).
    // Fires only when a boundary is crossed, so no latch is needed.
}

// inside your latched register function (see Mod Anatomy):
mmapi_on("renown.rank_gained", rank_banner_renown_rank_gained);
```

## Engine Wiring

- Seam [`renown_gains`](../seams/renown_gains.md) dispatches from `gml/scripts/GameplaySystems/Player/Ari.gml`, inside `set_renown()` below its gains-only early return, behind a rank-boundary comparison.

## See Also

- [renown.level_gained](renown.level_gained.md) - Know every renown level gain, boundary or not, from the same seam.
- [player.renown_delta](player.renown_delta.md) - Change every renown gain before it applies, upstream of this event.
