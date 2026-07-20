# Seam: taxi_room_transition_pre

Builds the room-transition ctx at the head of `taxi_player()` and writes handler edits back onto the itinerary.

`taxi_room_transition_pre` is a **text seam** (`anchor` + `replace`). It feeds [game.room_transition_pre](../hooks/game.room_transition_pre.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Locations/Taxi.gml` |
| **Locator** | text anchor at the head of `taxi_player(taxi_itinerary)`, after the world-room assert |
| **Feeds** | [`game.room_transition_pre`](../hooks/game.room_transition_pre.md) |
| **ctx built** | `{ taxi: self, itinerary: taxi_itinerary, from_room: room(), to_room: taxi_itinerary.gm_room, gm_room: taxi_itinerary.gm_room, instant: taxi_itinerary.instant }` |
| **Marker** | `mmapi_dungeon_run_room_transition_pre` |

## The Edit

The replace rewrites the head of `taxi_player(taxi_itinerary)`, before the transition starts. Right after the engine's world-room assert it builds `__mmapi_room_transition_ctx`: `taxi` (the Taxi struct, `self`), the `itinerary` argument, `from_room` (the current `room()`), and `to_room`/`gm_room`/`instant` read off the itinerary. It then emits `game.room_transition_pre`.

The write-back after the emit is what makes the redirect work. The seam reassigns `taxi_itinerary = ctx.itinerary`, copies `ctx.gm_room` back onto the itinerary, recomputes `taxi_itinerary.destination = gm_room_to_location_id(taxi_itinerary.gm_room)`, and copies `ctx.instant` back. A handler that mutates `ctx.gm_room` (or swaps in a whole new `ctx.itinerary`) therefore changes where the taxi actually goes. The destination is derived fresh from the possibly-edited room, never trusted from the handler. Emit and write-back sit in one try/catch, so a throwing handler leaves the transition on its original course.

`__mmapi_room_transition_ctx` is a plain local, deliberately long-lived: [taxi_room_transition_post](taxi_room_transition_post.md) reuses it at the end of the same function to emit the arrival event with the same struct.

## See Also

- [game.room_transition_pre](../hooks/game.room_transition_pre.md) - This is the hook that this seam dispatches.
- [taxi_room_transition_post](taxi_room_transition_post.md) - This is the arrival half of the same transition, and it reuses this seam's ctx local.
