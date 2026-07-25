# Seam: request_board_fetch_pool

Rewrites the request board's daily random top-up so the candidate pool and the draw cap pass through a filter.

`request_board_fetch_pool` is a **text seam** (`anchor` + `replace`). It feeds [request_board.fetch_pool](../hooks/request_board.fetch_pool.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/RequestBoard.gml` |
| **Locator** | text anchor on the whole of `create_request_board()`'s `if CALENDAR.time >= days(1)` block, the daily random top-up |
| **Op** | text (`anchor` + `replace`) |
| **Feeds** | [`request_board.fetch_pool`](../hooks/request_board.fetch_pool.md) |
| **Value filtered** | `{ random_pool, random_slots, year, season, day, is_crown_quest_day }` |
| **ctx built** | `undefined` (the struct rides in the value position) |
| **Marker** | `mmapi_request_board_fetch_pool_filter` |

## The Edit

The replacement rebuilds the top-up around one dispatch. It shuffles the candidate keys exactly as pristine does, builds the value struct (the shuffled List, the `misc/fetch_quests_per_day` cap, and the day context - every field a pure engine read), and threads it through `mmapi_apply_filters` in a try/catch. The re-reads are defensive, `dialogue.path`-style: a non-struct result is replaced with an empty struct, each field is read with the `[$ ]` accessor so a missing field falls back to the engine value, and the pool's `count()` is probed in its own try/catch so a replacement that is not a List degrades to an empty draw rather than a crash. The availability loop then runs pristine logic against the filtered pool and cap.

Two semantics worth naming: the pristine cap (`quantity`) stops the draw when the **whole board** reaches it - the crown offer and always-available entries pushed earlier count toward it - and the filter fires after `keys.shuffle()`, so the pool arrives pre-shuffled and a reorder by a handler sticks. With zero handlers the block is behaviorally equivalent to pristine: same seeded shuffle, same loop, same cap.

## See Also

- [request_board.fetch_pool](../hooks/request_board.fetch_pool.md) - This is the hook this seam dispatches.
- [request_board_fetch_pool_ready](request_board_fetch_pool_ready.md) - The companion emit at the end of the same build.
- [dialogue_path](dialogue_path.md) - The struct-filter shape whose defensive re-reads this seam follows.
