# Hook: ui.eod_calendar_events

Add to or trim the notifications under the end-of-day calendar.

`ui.eod_calendar_events` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires in `EodMenu.play_calendar_sequence()`, after the engine has gathered tomorrow's calendar events and before `build_notifications()` turns them into the rows under the calendar. The filtered value is `menu.events`, the List of event structs the rows are built from. ctx is `{ menu, target_date }`.

Mutate the List in place, return a replacement List, or return `undefined` to keep the current one. A return that is not a List is dropped and the engine's List stands. The notification block fades in only when the final List has entries, so adding to an empty List shows the rows and emptying it hides the block.

> [!IMPORTANT]
> The value is a **List** (`count()`, `get()`, `push()`, `insert()`), not an array. Push structs onto it or return a List, never a plain array.

| | |
| --- | --- |
| **Fires** | In `play_calendar_sequence()`, between the event gathering and `build_notifications()`, once per end-of-day sequence. |
| **Value** | The `menu.events` List of event structs. |
| **ctx** | `{ menu, target_date }` |
| **Kind contract** | The callback receives the current value and returns a replacement, or `undefined` to keep the current value. |

### The value List

Every vanilla entry is a struct with a `type` from the `CalendarEvent` enum and that type's payload, such as `{ type: CalendarEvent.Festival, festival }` or `{ type: CalendarEvent.NpcBirthday, npc }`. The engine gathers them in a fixed order. Festivals come first, then Friday night at the inn, the Saturday market, an available date, the wedding or anniversary, child birthdays, NPC birthdays, the player's birthday, and a legendary fish.

A custom entry is a struct `{ type, key, icon, npcs }`. Extra fields are ignored, so a mod may tag its own entries.

- `type` - a value no vanilla case matches. `CalendarEvent.LEN` is the convention. The row builder switches on this field, so it must be present.
- `key` - the localization key the row text resolves through `set_key()`. Wrap literal text with `ANCHOR.wrap_for_local()`, which is what the vanilla birthday rows do.
- `icon` - the sprite drawn to the left of the text. Vanilla rows use item icons, NPC icons, and small event icons here.
- `npcs` - optional. A boolean array with `NpcId.LEN` entries. When present the row gains the vanilla underline with a small icon for every `true` index and takes the taller row spacing.

### The ctx struct

- `menu` - the `EodMenu` running the sequence. Use it rather than `ANCHOR.get_menu(Menu.Eod)`.
- `target_date` - the calendar timestamp of the day about to begin, `CALENDAR.time + days(1)`. The engine passes the same value to `build_notifications()`. Read it with `get_seasons()`, `get_days()`, and `get_years()`.

## Usage

```gml
// ui.eod_calendar_events is a FILTER: you receive (value, ctx) and return a
// replacement, or undefined to keep the game's value.
function tomorrow_tips_eod_calendar_events(_value, _ctx) {
    // _value is the menu.events List. _ctx is { menu, target_date }.
    // One row per thing your mod knows about tomorrow, e.g.:
    // _value.push({
    //     type: CalendarEvent.LEN,
    //     key: "tomorrow_tips/something_tomorrow",
    //     icon: ITEM_PROTOTYPES[ItemId.Honeycomb].icon_sprite,
    // });
    return undefined; // undefined = keep the (possibly mutated) List
}

// inside your latched register function (see Mod Anatomy):
mmapi_filter("ui.eod_calendar_events", tomorrow_tips_eod_calendar_events);
```

Handlers run once per end of day, so ordinary lookups are fine. The world has not advanced yet, so anything about tomorrow is a prediction from today's state.

## Interactions

- Row text resolves through `set_key()`, so a custom `key` flows through [local.get](local.get.md) and, when it is not shipped as data, [local.missing](local.missing.md) can serve it at runtime.
- [ui.menu_opened](ui.menu_opened.md) fires for the `EodMenu` when the end-of-day summary spawns, before the player taps the Next Day button and long before this hook.
- [game.new_day](game.new_day.md) fires from `end_sequence()` after the calendar has played, so the state a handler reads here is still today's. The test suite's day skip calls `end_sequence()` directly and never builds the calendar.

## Engine Wiring

- Seam [`ui_eod_calendar_events`](../seams/ui_eod_calendar_events.md) dispatches from `gml/scripts/UI/Anchor/Menus/EodMenu.gml`, in `play_calendar_sequence()`, between the calendar face reset and `build_notifications()`.
- Companion seam [`ui_eod_notification_custom_entry`](../seams/ui_eod_notification_custom_entry.md) provides no dispatch of its own. It adds the `default` branch to the row builder's switch that reads `key`, `icon`, and `npcs` off an entry no vanilla case matches.

## See Also

- [ui.relationship_row_built](ui.relationship_row_built.md) - The other journal decoration point. It hands you finished rows to add nodes to, where this hook hands you the list the rows are built from.
- [ui.menu_opened](ui.menu_opened.md) - The open moment of the whole menu for the end-of-day summary.
- [game.new_day](game.new_day.md) - The day boundary that follows the calendar sequence.
- [local.missing](local.missing.md) - Serve a row's localization key at runtime.
