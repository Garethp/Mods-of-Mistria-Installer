# Hook: request_board.fetch_pool

Change the request board's daily candidate pool and draw cap before the random picks land.

`request_board.fetch_pool` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires in `create_request_board()`'s daily random top-up, after the crown offer and the always-available entries are already pushed and the candidate keys are shuffled, before the availability loop draws from them. The board builds once per day - new day, new game, and some loads - so this fires per build, not per viewing.

The filtered value is the struct `{ random_pool, random_slots, year, season, day, is_crown_quest_day }`. The ctx is `undefined`. Return the replacement struct, or `undefined` to keep the current values. The seam re-reads every field defensively, so a non-struct return or a partial struct keeps the engine values.

> [!IMPORTANT]
> `random_pool` is a **List** (`count()`/`get()`), not an array - return the List you were given (mutated or reordered), not a plain array. And `random_slots` caps the **whole board**: the draw stops when the board's total count reaches it, crown and always-available entries included, so lowering it can squeeze out the random picks entirely.

| | |
| --- | --- |
| **Fires** | In `create_request_board()`, after the candidate keys are shuffled and before the availability loop draws from them. Once per board build. |
| **Value** | `{ random_pool, random_slots, year, season, day, is_crown_quest_day }` |
| **ctx** | `undefined` |
| **Kind contract** | The callback receives the current value and returns a replacement, or `undefined` to keep the current value. |

### The value struct

- `random_pool` - the shuffled List of candidate request keys the draw iterates.
- `random_slots` - the total-board cap the draw stops at (`misc/fetch_quests_per_day`).
- `year` - the calendar year.
- `season` - the season index.
- `day` - the day within the season, 1-based.
- `is_crown_quest_day` - whether the crown cooldown is ready. An eligible crown offer may still be absent.

## Usage

```gml
// request_board.fetch_pool is a FILTER: you receive (value, ctx) and return a
// replacement, or undefined to keep the game's value.
function bigger_board_request_board_fetch_pool(_value, _ctx) {
    // _value is { random_pool, random_slots, year, season, day, is_crown_quest_day }.
    // random_pool is a List; random_slots caps the WHOLE board, not just the
    // random picks. _ctx is undefined.
    if (_value == undefined) return undefined; // test undefined BEFORE anything else
    _value.random_slots += 1; // room for one more request on today's board
    return _value;
}

mmapi_filter("request_board.fetch_pool", bigger_board_request_board_fetch_pool);
```

## Engine Wiring

- Seam [`request_board_fetch_pool`](../seams/request_board_fetch_pool.md) dispatches from `gml/scripts/RequestBoard.gml`, rewriting the daily random top-up in `create_request_board()` so the loop draws from the filtered pool and stops at the filtered cap.

## See Also

- [request_board.fetch_pool_ready](request_board.fetch_pool_ready.md) - Observe the finished board after the final availability pass.
- [game.new_day](game.new_day.md) - The day rollover this build rides on.
- [quest.complete](quest.complete.md) - Know when a quest is completed.
