# Hook: game.room_transition_pre

React to a room transition before it starts, and redirect it.

`game.room_transition_pre` is an **event** hook. Register a callback with `mmapi_on`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires at the top of `Taxi.taxi_player()`, before the transition starts. ctx is `{ taxi, itinerary, from_room, to_room, gm_room, instant }`. After the emit the seam writes `ctx.itinerary`, `ctx.gm_room`, and `ctx.instant` back onto the itinerary and recomputes its destination, so a handler can mutate the ctx to redirect the transition.

| | |
| --- | --- |
| **Fires** | At the top of `Taxi.taxi_player(taxi_itinerary)`, before the transition starts. |
| **ctx** | `{ taxi, itinerary, from_room, to_room, gm_room, instant }` |
| **Kind contract** | The callback observes the moment. Its return value is ignored. |

### The ctx struct

- `taxi` - the `Taxi` struct itself, the location system running the transition. It is informational and is not written back.
- `itinerary` - the `taxi_itinerary` passed to `taxi_player()`. **Written back**: after the emit the seam reassigns the local from `ctx.itinerary`, so replacing this field swaps in a whole different itinerary.
- `from_room` - `room()` at the moment the transition starts: the room being left. It is informational and is not written back.
- `to_room` - the itinerary's `gm_room` as the transition begins: where the taxi is headed. It is informational and is not written back. Mutate `gm_room` to redirect.
- `gm_room` - the destination `gm_room`, same starting value as `to_room`. **Written back** onto the itinerary. The seam then recomputes `itinerary.destination` with `gm_room_to_location_id(gm_room)`, so setting this one field redirects the whole transition consistently.
- `instant` - the itinerary's `instant` flag: whether the transition skips the travel presentation. **Written back**.

> [!IMPORTANT]
> This event has a mutable ctx. Your return value is still ignored. Redirect by mutating the struct you are given. Set `ctx.gm_room` to send the player somewhere else (the seam recomputes the itinerary's `destination` from it). Set `ctx.instant` to make the transition immediate or not. Mutating `ctx.to_room` or `ctx.from_room` does nothing.

## Usage

```gml
// game.room_transition_pre is an EVENT with a mutable ctx: the return
// value is ignored, but the seam writes ctx.itinerary, ctx.gm_room, and
// ctx.instant back onto the itinerary after you run.
function taxi_detour_game_room_transition_pre(_ctx) {
    // _ctx is { taxi, itinerary, from_room, to_room, gm_room, instant }.
    //   .taxi      - the Taxi struct running the transition (read-only).
    //   .itinerary - the taxi_itinerary; written back, replace to swap it.
    //   .from_room - room() being left (read-only).
    //   .to_room   - the destination gm_room at fire time (read-only).
    //   .gm_room   - the destination; written back, destination recomputed.
    //   .instant   - skip the travel presentation; written back.
    // if (_ctx.to_room == <somewhere your mod gates>) {
    //     _ctx.gm_room = <your replacement gm_room>; // redirect
    //     _ctx.instant = true;                        // and make it snappy
    // }
}

// inside your latched register function (see Mod Anatomy):
mmapi_on("game.room_transition_pre", taxi_detour_game_room_transition_pre);
```

## Engine Wiring

- Seam [`taxi_room_transition_pre`](../seams/taxi_room_transition_pre.md) dispatches from `gml/scripts/GameplaySystems/Locations/Taxi.gml`, at the top of `taxi_player()`. After the emit it writes `itinerary`, `gm_room`, and `instant` back onto the itinerary and recomputes the destination via `gm_room_to_location_id()`.

## See Also

- [game.room_transition_post](game.room_transition_post.md) - This is the end of the same transition, with the same ctx struct re-read from the live itinerary.
- [game.room_changed](game.room_changed.md) - This is the after-the-fact derived event, which fires once the room has actually changed. It is a lagging observation, never a state source; this hook is where request-time semantics live.
- [dungeon.floor_enter](dungeon.floor_enter.md) - This is the dungeon-side entry bracket, for changing dungeon floor content as it loads.
