# Seam: taxi_room_transition_post

Re-reads the live itinerary at the end of the taxi transition and emits the arrival event.

`taxi_room_transition_post` is a **text seam** (`anchor` + `replace`). It feeds [game.room_transition_post](../hooks/game.room_transition_post.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Locations/Taxi.gml` |
| **Locator** | text anchor at the tail of the taxi transition: the `MUSIC_PLAYER` selector block plus the closing brace |
| **Feeds** | [`game.room_transition_post`](../hooks/game.room_transition_post.md) |
| **ctx built** | the same `__mmapi_room_transition_ctx` struct as the pre seam, with `itinerary`, `to_room`, `gm_room`, and `instant` re-read from `self.itinerary` |
| **Marker** | `mmapi_dungeon_run_room_transition_post` |
| **Depends on** | [`taxi_room_transition_pre`](taxi_room_transition_pre.md) |

## The Edit

The anchor is the last block of the taxi transition (the `MUSIC_PLAYER` selector check) plus the closing brace, so the injected code is the final thing the transition runs, after arrival and music selection. Just before its emit the seam re-reads the live itinerary into the ctx: `itinerary` becomes `self.itinerary`, and `to_room`, `gm_room`, and `instant` are read fresh from it. Whatever the pre handlers (or the engine) did to the itinerary in between, post handlers see where the transition actually ended up. The `from_room` captured at departure is left untouched, so origin and destination are both on the struct.

The emit reuses `__mmapi_room_transition_ctx`, the local that [taxi_room_transition_pre](taxi_room_transition_pre.md) declares at the head of the same function. Hence `depends_on = ["taxi_room_transition_pre"]`: without pre's edit the local would not exist. Apply order is catalog order plus `depends_on` edges, so the pre seam is always staged first. The whole re-read-and-emit is wrapped in its own try/catch. A throwing handler cannot disturb the completed transition.

## See Also

- [game.room_transition_post](../hooks/game.room_transition_post.md) - This is the hook that this seam dispatches.
- [taxi_room_transition_pre](taxi_room_transition_pre.md) - This seam is the departure half. It builds the shared ctx and makes redirects work.
