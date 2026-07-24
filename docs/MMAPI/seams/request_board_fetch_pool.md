# Seam: request_board_fetch_pool

Filters the random candidate pool used by request-board generation in `create_request_board()`.

`request_board_fetch_pool` is a **text seam** that feeds [request_board.fetch_pool](../hooks/request_board.fetch_pool.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/RequestBoard.gml` |
| **Function** | `create_request_board()` |
| **Locator** | inside `if CALENDAR.time >= days(1)` after `var keys = ListFromArray(REQUEST_BOARD_ENTRIES.keys());` and before the randomized iteration |
| **Op** | filter |
| **ctx fields** | `random_pool`, `random_slots`, `year`, `season`, `day`, `is_crown_quest_day` |
| **Marker** | `mmapi_request_board_fetch_pool_filter` |

## The Edit

The seam replaces the request-board's in-place randomization loop so mods can:

- reorder or prune candidates before selection
- adjust the number of random slots
- replace the pool entirely for that day

The request is still filtered for availability and request log state by the wrapped vanilla logic.

## See Also

- [request_board.fetch_pool](../hooks/request_board.fetch_pool.md)
- [request_board.fetch_pool_ready](../hooks/request_board.fetch_pool_ready.md)
