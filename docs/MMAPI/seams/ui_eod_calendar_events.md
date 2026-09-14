# Seam: ui_eod_calendar_events

Filters the end-of-day calendar's event List between its gathering and the row build.

`ui_eod_calendar_events` is a **text seam** (`anchor` + `replace`). It feeds [ui.eod_calendar_events](../hooks/ui.eod_calendar_events.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/UI/Anchor/Menus/EodMenu.gml` |
| **Locator** | text anchor on the `set_calendar_time(CALENDAR.time)` and `build_notifications(target_date)` pair in `play_calendar_sequence()` |
| **Op** | text (`anchor` + `replace`) |
| **Feeds** | [`ui.eod_calendar_events`](../hooks/ui.eod_calendar_events.md) |
| **Value filtered** | `self.events`, the List of calendar event structs |
| **ctx built** | `{ menu: self, target_date: target_date }` |
| **Marker** | `mmapi_ui_eod_calendar_events_filter` |

## The Edit

Pristine `play_calendar_sequence()` fills `self.events` with tomorrow's festivals, market days, dates, birthdays, and the legendary fish, resets the calendar face to today, and calls `build_notifications(target_date)` to draw one row per entry. The replacement inserts the dispatch between the reset and the row build. It reads the List into a local, threads it through `mmapi_apply_filters` under a site-level catch, and probes the result before writing it back. The result must be a struct whose `count()` answers a number, which is what a List is, and a second catch turns a failed probe into keeping the engine's List. In the [request_board_fetch_pool](request_board_fetch_pool.md) style, a return that is not a List therefore falls back to pristine instead of crashing the sequence.

The write-back lands before every read of the List. `build_notifications()` draws from it next, and the chain built below tests `is_empty()` on it to decide whether the notification block fades in, so a handler's additions and removals reach both.

With zero handlers the filter returns the same List, the probe passes, and the write-back stores the List that was already there, so the seam is behaviorally identical to pristine. The dispatch runs once per end-of-day sequence.

## See Also

- [ui.eod_calendar_events](../hooks/ui.eod_calendar_events.md) - This is the hook this seam dispatches.
- [ui_eod_notification_custom_entry](ui_eod_notification_custom_entry.md) - The companion edit that lets the row builder draw an entry the filter added.
- [request_board_fetch_pool](request_board_fetch_pool.md) - The List probe this seam follows.
- [ui_relationship_row_built](ui_relationship_row_built.md) - The sibling journal seam, a text seam in another menu.
