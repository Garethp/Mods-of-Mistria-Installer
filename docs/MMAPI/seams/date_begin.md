# Seam: date_begin

Guards `start_date_cutscene` inside `run_date`, after the acceptance conversation.

`date_begin` is a **text seam** (guard-shaped: it dispatches `mmapi_check_guards`). It feeds [date.begin](../hooks/date.begin.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/Dates.gml` |
| **Locator** | text anchor on the `play_conversation` callback in `run_date(date, npc)` |
| **Op** | text (guard dispatch) |
| **Feeds** | [`date.begin`](../hooks/date.begin.md) |
| **ctx built** | `{ npc: npc, date: date }` |
| **Marker** | `mmapi_date_begin_guards` |

## The Edit

One line guards the `start_date_cutscene(npc, date)` call inside `run_date`'s acceptance-conversation callback:

```gml
    play_conversation(npc, date_convo, function(_, npc, date) {
        if (mmapi_check_guards("date.begin", { npc: npc, date: date }) == false) { return; } // mmapi_date_begin_guards
        start_date_cutscene(npc, date);
    }, [npc, date]);
```

The callback runs after the `DateAcceptance` conversation has played and its `writes` have applied. If any registered guard returns `false`, the callback returns early and `start_date_cutscene` is skipped, so the accepted date is cancelled. When every guard allows, the date begins as normal.

This dispatch lives in `run_date`'s own body, so it only fires when that body runs. A date claimed by the [`date_run`](date_run.md) seam's override (which returns before this point) never reaches it.

## See Also

- [date.begin](../hooks/date.begin.md) - This is the hook this seam dispatches.
- [date_run](date_run.md) - The head-of-`run_date` override seam, which fires before the acceptance conversation.
