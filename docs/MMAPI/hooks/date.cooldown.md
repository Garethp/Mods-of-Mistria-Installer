# Hook: date.cooldown

Change the cooldown between dates with the same NPC.

`date.cooldown` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires in `ari_eligible_for_date(npc, time_to_check)` (`Dates.gml`), once per eligibility check, before the date history is scanned. The filtered value is the date cooldown in game seconds, defaulting to `days(6)`, the cooldown the engine enforces between listed dates with the same NPC. ctx is `{ npc, time_to_check }`: `npc` is the `NpcId` being checked, `time_to_check` is the calendar time the check is for (usually now). Return a replacement duration, or `undefined` to keep the current value.

Return `0` to remove the per NPC cooldown. This does not allow a second date on the same day. A separate engine clause beside the cooldown rejects any date whose timestamp matches an existing history entry, and `CALENDAR.time` advances a whole day at a time, so every date taken on one day carries the same timestamp. That clause is not scoped to an NPC either, so once any date is taken, no further date is possible that day whatever this hook returns. A non-numeric return is dropped with a rate-limited warning and the vanilla cooldown applies.

Eligibility gates whether the Date option appears at all, so this hook runs upstream of [date.run](date.run.md) and fires from the talk menu and the date eligibility UI.

> [!WARNING]
> This is a hot hook that may fire repeatedly. Keep the handler cheap, return early, and never log or read from a file inside it without latching.

| | |
| --- | --- |
| **Fires** | Inside `ari_eligible_for_date`, before the date history scan. |
| **Value** | The cooldown in game seconds, defaulting to `days(6)`. |
| **ctx** | `{ npc, time_to_check }` - the `NpcId` and the calendar time being checked. |
| **Kind contract** | Chained filter: return a replacement duration, or `undefined` to keep the current value. Later filters receive the current result. |

### The ctx struct

- `npc` - the `NpcId` whose eligibility is being checked.
- `time_to_check` - the calendar time the check is for, defaulting to `CALENDAR.time`.

## Usage

```gml
// date.cooldown is a FILTER: return a replacement cooldown in game seconds,
// or undefined to keep the current value (the vanilla six days, or another
// mod's earlier replacement).
function frequent_dates_date_cooldown(_value, _ctx) {
    return days(1); // one day between dates with the same NPC
}

// Inside your latched register function (see Mod Anatomy):
mmapi_filter("date.cooldown", frequent_dates_date_cooldown);
```

`days(n)` is the engine's own conversion to game seconds, so express durations with it rather than raw numbers. Gate on `_ctx.npc` to shorten the cooldown for one partner and leave the rest vanilla.

## Interactions

- The cooldown is one of several eligibility gates. Date days (`misc/date_days`), eligible statuses (`misc/statuses_eligible_for_dates`), and heart points (`misc/date_heart_points`) are fiddle data. Change those with a fiddle merge, not a handler.
- Dates flagged `unlisted` never enter the date history, so they neither consume nor respect the cooldown.
- Festival days and the day of the week gate still block dates regardless of the cooldown.
- One date per day is enforced separately from the cooldown and cannot be lifted from here. Dates are stamped with `CALENDAR.time`, which changes only at a day boundary, so the engine's timestamp match rejects every later date that day for every NPC.

## Engine Wiring

- Seam [`date_cooldown`](../seams/date_cooldown.md) dispatches from `gml/scripts/Dates.gml`, at the head of `ari_eligible_for_date`, and threads the filtered duration into the history comparison.

## See Also

- [date.run](date.run.md) - Take over a date the moment the player commits to it, after eligibility has passed.
- [date.cutscene](date.cutscene.md) - Swap which cutscene an accepted date plays.
