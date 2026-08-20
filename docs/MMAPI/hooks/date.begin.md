# Hook: date.begin

Cancel an accepted date after its acceptance conversation, before the cutscene.

`date.begin` is a **guard** hook. Register a callback with `mmapi_guard`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires inside run_date(date, npc) (Dates.gml), in the DateAcceptance conversation's completion callback and right after that conversation has played and the writes have applied and immediately before start_date_cutscene(npc, date). ctx is { npc, date }: npc is the NpcId, date is the Date enum id. Return false to cancel the accepted date (start_date_cutscene is skipped, so no date cutscene plays). Any other return lets the date begin.

Because it lives in `run_date`'s own body, it only fires when that body runs: a date claimed by a `date.run` override (which replaces `run_date` entirely) never reaches this guard. A `date.run` override that wants to honour decline can dispatch the guard itself with `mmapi_check_guards("date.begin", { npc, date })` before starting the cutscene.

| | |
| --- | --- |
| **Fires** | Inside `run_date`, in the acceptance conversation's completion callback, right before `start_date_cutscene`. |
| **ctx** | `{ npc, date }` — the `NpcId` and the `Date` enum id. |
| **Kind contract** | Only the Boolean value `false` skips the cutscene. Every other return allows. Guards fail open: a callback that throws counts as allow. |

### The ctx struct

- `npc` - the `NpcId` of the date partner.
- `date` - the `Date` enum id (index into `DATES`).

## Usage

```gml
// date.begin is a GUARD: return Boolean false to cancel the date whilst every other
// return allows. Guards fail OPEN and if your handler crashes, the date happens.
function date_preferences_date_begin(_ctx) {
    // _ctx is { npc, date }. The acceptance conversation has already played and
    // its writes have applied, so a refusal line can flag the decline here.
    if (T2R.read(format("{NpcId}_refused_date", _ctx.npc)) == true) {
        return false; // the NPC declined - skip start_date_cutscene
    }
    return undefined; // allow the date to begin
}

// inside your latched register function (see Mod Anatomy):
mmapi_guard("date.begin", date_preferences_date_begin);
```

## Engine Wiring

- Seam [`date_begin`](../seams/date_begin.md) dispatches from `gml/scripts/Dates.gml`, guarding the `start_date_cutscene(npc, date)` call inside `run_date`'s `play_conversation` callback.

## See Also

- [date.run](date.run.md) - Take over a date at `run_date`'s head, before the acceptance conversation.
- [dialogue.play_guard](dialogue.play_guard.md) - Decline the acceptance conversation itself before it plays.
