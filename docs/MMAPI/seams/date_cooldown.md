# Seam: date_cooldown

Threads the date cooldown through a filter before the eligibility scan uses it.

`date_cooldown` is a **text seam** (`anchor` + `replace`). It feeds [date.cooldown](../hooks/date.cooldown.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/Dates.gml` |
| **Locator** | text anchor from the head of `ari_eligible_for_date(npc, time_to_check)` through the history comparison |
| **Op** | text (filter dispatch) |
| **Feeds** | [`date.cooldown`](../hooks/date.cooldown.md) |
| **Value filtered** | the cooldown in game seconds, defaulting to `days(6)` |
| **ctx built** | `{ npc: npc, time_to_check: time_to_check }` |
| **Marker** | `mmapi_date_cooldown_filter` |

## The Edit

The pristine site compares each history entry against a literal:

```gml
    if time_to_check == v.timestamp
        || (v.npc == npc && time_to_check - v.timestamp < days(6))
```

The seamed site hoists the literal into `__mmapi_date_cooldown` before the loop, filters it once per eligibility check, validates the result, and uses it in the comparison:

```gml
    var __mmapi_date_cooldown = days(6);
    try { __mmapi_date_cooldown = mmapi_apply_filters("date.cooldown", __mmapi_date_cooldown, { npc: npc, time_to_check: time_to_check }); } catch (__mmapi_date_cooldown_err) {} // mmapi_date_cooldown_filter
    if (is_numeric(__mmapi_date_cooldown) == false) {
        mmapi_warn_rate_limited(...);
        __mmapi_date_cooldown = days(6);
    }
```

The dispatch fails open twice over. The filter call sits under its own try/catch, and a non-numeric result falls back to the vanilla duration with a rate-limited warning under the `mmapi` label. The comparison would throw on a non-numeric operand, and eligibility runs from the talk menu, so the type check is what keeps a broken handler from crashing a conversation.

The filter runs once per eligibility call rather than once per history entry, so a handler sees one dispatch however long the date history is. The timestamp equality beside the comparison is untouched. Because `CALENDAR.time` advances only at a day boundary, that clause matches any date already taken today and rejects the new one for every NPC, so a cooldown of `0` removes the per NPC spacing without permitting a second date the same day.

When no handler is registered, or every handler declines, `__mmapi_date_cooldown` equals `days(6)` and the seamed code is behavior-identical to pristine.

## See Also

- [date.cooldown](../hooks/date.cooldown.md) - This is the hook this seam dispatches.
- [date_run](date_run.md) - The override seam that fires once an eligible date is chosen.
