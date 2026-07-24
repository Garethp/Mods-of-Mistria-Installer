# Hook: request_board.fetch_pool_ready

React when the request-board random pool is finalized.

`request_board.fetch_pool_ready` is an **event** hook. Register a callback with `mmapi_on`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires immediately after request-board randomization and filtering are complete for the day.

`ctx` is the same payload as `request_board.fetch_pool`, plus:

- `final_pool`: the final post-filter pool that will be returned

Its return value is ignored.

| | |
| --- | --- |
| **Fires** | After random-pool filtering and finalization, before the board is shown. |
| **ctx** | `{ random_pool, random_slots, year, season, day, is_crown_quest_day, final_pool }` |
| **Kind contract** | Event callback observes completion only; return values are ignored. |

## Usage

```gml
// request_board.fetch_pool_ready is an EVENT: return is ignored.
function my_request_board_ready(_ctx) {
    // _ctx.final_pool is the board's post-filter final candidates.
}

mmapi_on("request_board.fetch_pool_ready", my_request_board_ready);
```

## Engine Wiring

- This hook is implemented by the [request_board_fetch_pool_ready](../seams/request_board_fetch_pool_ready.md) text seam, which emits after final board list construction in `RequestBoard.gml`.

## See Also

- [request_board.fetch_pool](request_board.fetch_pool.md) - Filter or rewrite candidates before finalization.
