# Seam: ui_eod_notification_custom_entry

Adds the row builder's default branch so an entry added by a filter declares its own key, icon, and NPCs.

`ui_eod_notification_custom_entry` is a **text seam** and a **companion edit**. It dispatches nothing itself. It exists for [ui.eod_calendar_events](../hooks/ui.eod_calendar_events.md), whose dispatch lives in [ui_eod_calendar_events](ui_eod_calendar_events.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/UI/Anchor/Menus/EodMenu.gml` |
| **Locator** | text anchor on the tail of the `switch event.type` in `build_notifications()`, from the last case's `break` through the `var text = ANCHOR.text(...)` line that follows the switch |
| **Op** | text (`anchor` + `replace`) |
| **Feeds** | [`ui.eod_calendar_events`](../hooks/ui.eod_calendar_events.md) (no dispatch of its own) |
| **Marker** | `mmapi_ui_eod_notification_custom_entry` |

## The Edit

`build_notifications()` walks `self.events` and resolves each entry's `icon`, `key`, and `npcs` locals through a `switch` over the `CalendarEvent` enum. The switch has no `default`, so an entry whose `type` matches no case would reach the row build with all three undefined. The replacement appends a `default` branch that reads the three off the entry struct with the `[$ ]` accessor, so an absent optional field stays undefined rather than throwing. Everything after the switch is untouched, and the custom row gets the vanilla text node, icon node, optional NPC underline, row spacing, and its share of the calendar recentering.

Every vanilla entry matches a case, so with zero handlers the branch never runs and the seam leaves vanilla behavior intact. The two `EodMenu.gml` seams touch different functions and neither re-emits the other's text, so they carry no dependency edge and apply in catalog order.

## See Also

- [ui.eod_calendar_events](../hooks/ui.eod_calendar_events.md) - This is the hook this companion edit serves.
- [ui_eod_calendar_events](ui_eod_calendar_events.md) - This is the dispatching seam whose entries this branch draws.
- [fish_chest_table_lookup](fish_chest_table_lookup.md) - The other shipped edit that adds a `default` branch to an engine switch.
