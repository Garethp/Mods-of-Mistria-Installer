# Seam: date_run

Puts a claim-scoped override in front of every player-initiated date.

`date_run` is a **text seam** (override-shaped: it dispatches `mmapi_run_override`). It feeds [date.run](../hooks/date.run.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/Dates.gml` |
| **Locator** | text anchor at the head of `run_date(date, npc)` |
| **Op** | text (override dispatch) |
| **Feeds** | [`date.run`](../hooks/date.run.md) |
| **ctx built** | `{ date: date, npc: npc }` |
| **Marker** | `mmapi_date_run_override` |

## The Edit

Two injected lines land at the head of `run_date(date, npc)`, before the `DateAcceptance` conversation is requested:

```gml
var __mmapi_date_run = mmapi_run_override("date.run", { date: date, npc: npc }); // mmapi_date_run_override
if (__mmapi_date_run != undefined) { return __mmapi_date_run; }
```

A non-`undefined` return short-circuits the function: the engine's `run_date` for this date is skipped entirely (its acceptance conversation and NPC posing never run) and the override's value becomes `run_date`'s return (return `true` for a handled date). When every handler defers (`undefined`), execution falls straight through into the engine's normal handling.

`run_date()` is the single entry point the date-selection UI routes every date through (`await_popup(run_date, [date, npc])`), so the override sees every player-initiated date. ctx is `{ date, npc }`. The hook is claim-scoped: many mods register, each returning `undefined` for the dates it does not own, and the first non-`undefined` return claims the date.

## See Also

- [date.run](../hooks/date.run.md) - This is the hook this seam dispatches.
