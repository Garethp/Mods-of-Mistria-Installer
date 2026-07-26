# Hook: request_board.fetch_pool_ready

Know the finished request board the moment it is built each day.

`request_board.fetch_pool_ready` is an **event** hook. Register a callback with `mmapi_on`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires at the end of `create_request_board()`, after the final retain pass has dropped completed and active quests and the RNG is re-randomized, just before the built board list is returned. The board builds once per day - new day, new game, and some loads - so this fires per build, not per viewing.

ctx is `{ year, season, day, is_crown_quest_day, final_pool }`. The date fields match [request_board.fetch_pool](request_board.fetch_pool.md)'s. The return value is ignored.

> [!IMPORTANT]
> `final_pool` is the **live List** the function returns - pushing or removing entries here changes the actual board. Treat it as read-only unless that is the intent; to shape the board's candidates or size, prefer [request_board.fetch_pool](request_board.fetch_pool.md).

| | |
| --- | --- |
| **Fires** | At the end of `create_request_board()`, after the final retain pass, before the board list is returned. Once per board build. |
| **ctx** | `{ year, season, day, is_crown_quest_day, final_pool }` |
| **Kind contract** | Every handler runs. Return values are ignored. |

### The ctx struct

- `year` - the calendar year.
- `season` - the season index.
- `day` - the day within the season, 1-based.
- `is_crown_quest_day` - whether the crown cooldown is ready. An eligible crown offer may still be absent.
- `final_pool` - the live List of request keys the board will carry today.

## Usage

```gml
// request_board.fetch_pool_ready is an EVENT: the return value is ignored.
function board_watcher_request_board_fetch_pool_ready(_ctx) {
    // _ctx is { year, season, day, is_crown_quest_day, final_pool }.
    // final_pool is the LIVE board List - read it, do not reshape it here.
    mmapi_log_info("board_watcher", "board built with " + string(_ctx.final_pool.count()) + " requests");
}

mmapi_on("request_board.fetch_pool_ready", board_watcher_request_board_fetch_pool_ready);
```

## Engine Wiring

- Seam [`request_board_fetch_pool_ready`](../seams/request_board_fetch_pool_ready.md) dispatches from `gml/scripts/RequestBoard.gml`, at the tail of `create_request_board()`, after the retain pass and `randomize()`.

## See Also

- [request_board.fetch_pool](request_board.fetch_pool.md) - Change the candidates and cap before the draw instead of watching the result.
- [game.new_day](game.new_day.md) - The day rollover this build rides on.
