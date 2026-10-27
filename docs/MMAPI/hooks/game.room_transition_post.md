# Hook: game.room_transition_post

Know the settled destination of a room transition before the room changes.

`game.room_transition_post` is an **event** hook. Register a callback with `mmapi_on`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires at the end of `Taxi.taxi_player()`, once the transition is under way but still in the room being left. The room changes on a later frame, so this is not an arrival moment. For that, use [game.room_changed](game.room_changed.md). ctx is the same struct as [game.room_transition_pre](game.room_transition_pre.md) (`{ taxi, itinerary, from_room, to_room, gm_room, instant }`), with `itinerary`, `to_room`, `gm_room`, and `instant` re-read from the live itinerary just before the emit, after any pre redirect.

| | |
| --- | --- |
| **Fires** | At the end of `Taxi.taxi_player(taxi_itinerary)`, once the transition is under way but still in the room being left. |
| **ctx** | `{ taxi, itinerary, from_room, to_room, gm_room, instant }` |
| **Kind contract** | The callback observes the moment. Its return value is ignored. |

### The ctx struct

This is the same struct object the pre event dispatched, with the destination fields re-read from the taxi's live itinerary just before the emit, so you see where the transition will go, including any redirect a `game.room_transition_pre` handler made.

- `taxi` - the `Taxi` struct that ran the transition, as captured at the pre stage.
- `itinerary` - re-read: the taxi's live `itinerary` once the transition is under way.
- `from_room` - the room the transition left, as captured at the pre stage and not re-read.
- `to_room` - re-read: the live itinerary's `gm_room`, the room the transition will land in.
- `gm_room` - re-read: the same live `gm_room` value as `to_room`.
- `instant` - re-read: the live itinerary's `instant` flag.

> [!NOTE]
> Unlike the pre event, this one is purely after the fact: the seam performs no write-back, so mutating the ctx here changes nothing.

## Usage

```gml
// game.room_transition_post is an EVENT: the return value is ignored.
// You cannot change or stop it here; the return value is ignored.
function departure_bell_game_room_transition_post(_ctx) {
    // _ctx is { taxi, itinerary, from_room, to_room, gm_room, instant }.
    //   .taxi      - the Taxi struct that ran the transition.
    //   .itinerary - the live itinerary once the transition is under way (re-read at emit).
    //   .from_room - the room that was left (captured at the pre stage).
    //   .to_room   - the gm_room the transition will land in (re-read at emit).
    //   .gm_room   - same re-read value as to_room.
    //   .instant   - whether the transition ran instantly (re-read at emit).
    // your departure code here
}

// inside your latched register function (see Mod Anatomy):
mmapi_on("game.room_transition_post", departure_bell_game_room_transition_post);
```

## Engine Wiring

- Seam [`taxi_room_transition_post`](../seams/taxi_room_transition_post.md) dispatches from `gml/scripts/GameplaySystems/Locations/Taxi.gml`, at the end of `taxi_player()`, after the music selector block and before the room swap runs. It depends on [`taxi_room_transition_pre`](../seams/taxi_room_transition_pre.md), whose ctx struct it re-reads and re-emits.

## See Also

- [game.room_transition_pre](game.room_transition_pre.md) - This is the start of the same transition, where mutating the ctx can redirect it.
- [game.room_changed](game.room_changed.md) - This is the derived room change event from the begin_step poll, and the place to observe the arrival.
- [audio.music_selector](audio.music_selector.md) - This is the dungeon music choice that runs during the transition's music selection.
