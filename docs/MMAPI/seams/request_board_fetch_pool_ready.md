# Seam: request_board_fetch_pool_ready

Emits the finished request board at the tail of the daily build, final pool included.

`request_board_fetch_pool_ready` is a **text seam** (`anchor` + `replace`). It feeds [request_board.fetch_pool_ready](../hooks/request_board.fetch_pool_ready.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/RequestBoard.gml` |
| **Locator** | text anchor on `create_request_board()`'s tail: the `randomize()` line and the `return requests` that closes the function |
| **Op** | text (`anchor` + `replace`) |
| **Feeds** | [`request_board.fetch_pool_ready`](../hooks/request_board.fetch_pool_ready.md) |
| **Value filtered** | none - an emit |
| **ctx built** | `{ year, season, day, is_crown_quest_day, final_pool }` |
| **Marker** | `mmapi_request_board_fetch_pool_ready_emit` |

## The Edit

The emit lands between `randomize()` and the return, inside a try/catch, after the final `retain` pass has already dropped completed and active quests - so `final_pool` is exactly the list the function returns. It is the **live** List, not a copy: a handler that mutates it changes the actual board, which the hook page documents as read-only-unless-intended. The date fields are the same pure engine reads the companion filter's value carries. Emitting after `randomize()` means the deterministic per-day seed used by the build has already been discarded; handlers observing here see the finished result, not the seeded stream.

## See Also

- [request_board.fetch_pool_ready](../hooks/request_board.fetch_pool_ready.md) - This is the hook this seam dispatches.
- [request_board_fetch_pool](request_board_fetch_pool.md) - The companion filter earlier in the same build.
- [quest_complete](quest_complete.md) - This is the emit inside a validated quest completion.
