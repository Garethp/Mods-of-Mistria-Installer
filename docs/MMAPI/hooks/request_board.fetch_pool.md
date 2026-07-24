# Hook: request_board.fetch_pool

Filter request-board candidates before the daily random draw.

`request_board.fetch_pool` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires during request-board generation after non-crown candidates are collected and before the board draws from the random pool.

The filtered value is a struct with these fields:

- `random_pool`: array of candidate request IDs for random slots
- `random_slots`: number of random slots the board is selecting
- `year`: current year
- `season`: current season index
- `day`: current day within season
- `is_crown_quest_day`: whether crown slots are currently available

| | |
| --- | --- |
| **Fires** | In request-board generation, after candidates are assembled and before random selection. |
| **Filtered value** | `{ random_pool, random_slots, year, season, day, is_crown_quest_day }` |
| **ctx** | Same struct as the filtered value. |
| **Kind contract** | Return a replacement struct or `undefined` to keep behavior. |

## Usage

```gml
// request_board.fetch_pool is a FILTER: return struct or undefined.
function my_request_board_filter(_pool_ctx) {
    // Example: reserve one slot for custom content.
    // if (_pool_ctx.random_slots > 0) {
    //     _pool_ctx.random_slots -= 1;
    //     return _pool_ctx;
    // }
    return undefined;
}

mmapi_filter("request_board.fetch_pool", my_request_board_filter);
```

## Engine Wiring

- This hook is implemented by the [request_board_fetch_pool](../seams/request_board_fetch_pool.md) text seam, which reads `RequestBoard.gml` and dispatches after the random pool is assembled.

## See Also

- [request_board.fetch_pool_ready](request_board.fetch_pool_ready.md) - Observe the final pool after randomization.
