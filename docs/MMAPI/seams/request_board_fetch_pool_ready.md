# Seam: request_board_fetch_pool_ready

Emits after request-board finalization, so mods can observe the exact board output for the day.

`request_board_fetch_pool_ready` is an **emit seam** that feeds [request_board.fetch_pool_ready](../hooks/request_board.fetch_pool_ready.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/RequestBoard.gml` |
| **Function** | `create_request_board()` |
| **Locator** | after `randomize();` and before `return requests;` |
| **Op** | event emit |
| **ctx fields** | `{ random_pool, random_slots, year, season, day, is_crown_quest_day, final_pool }` |
| **Marker** | `mmapi_request_board_fetch_pool_ready_emit` |

## The Edit

After the request list is finalized, the seam emits `request_board.fetch_pool_ready` with the same day context as `request_board.fetch_pool`, plus `final_pool` so observers can reason about exactly what the player will see.

## See Also

- [request_board.fetch_pool](../hooks/request_board.fetch_pool.md)
- [request_board.fetch_pool_ready](../hooks/request_board.fetch_pool_ready.md)
