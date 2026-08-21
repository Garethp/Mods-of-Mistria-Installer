# Hook: date.begin

Cancel an accepted date after its acceptance conversation, before the cutscene.

`date.begin` is a **guard** hook. Register a callback with `mmapi_guard`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires inside `run_date(date, npc)` (`Dates.gml`), in the `DateAcceptance` conversation's completion callback. The conversation has already played and its `writes` have applied by then, and `start_date_cutscene(npc, date)` is what runs next. ctx is `{ npc, date }`: `npc` is the `NpcId`, `date` is the `Date` enum id. Return `false` to cancel the accepted date, which skips `start_date_cutscene` so no date cutscene plays. Any other return lets the date begin.

Because it lives in `run_date`'s own body, it only fires when that body runs: a date claimed by a `date.run` override (which replaces `run_date` entirely) never reaches this guard. A `date.run` override that wants to respect the decline can dispatch the guard itself with `mmapi_check_guards("date.begin", { npc, date })` before starting the cutscene.

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

## Interactions

- A vetoed date has already committed its acceptance writes. Whichever `date_acceptance` entry played is the one whose `writes` applied, even though the date was cancelled. Progression counters advance and the follow-up dialogue arms, so the NPC will reference a date that never happened. Keep a refusal entry's writes to your own facts. See [Have an NPC Refuse a Date](#have-an-npc-refuse-a-date) below for the full pattern.
- A vetoed date consumes nothing downstream. No `date_history` entry is written, no cooldown begins, and no rewards are granted, so the player can ask again.

## Have an NPC Refuse a Date

Three pieces must be implemented together:
1. A refusal acceptance entry that says no and writes only your own fact.
2. A requires override that keeps the vanilla acceptance entries from matching under your refusal conditions.
3. A `date.begin` guard keyed on your fact.

```toml
# t2/Conversations/Bank/Celine/Date Lines/beach.c.toml

# The refusal entry. Its writes carry ONLY your own fact, because these
# writes commit even when a guard cancels the date.
[beach_refuse_storm]
   kind = "date_acceptance"
   requires = [
      { npc = "celine" },
      { date = "beach" },
      { weather = "rainy" },
   ]
   writes = [
      { celine_refused_beach_date = true, expires = "1d" },
   ]
   speaker = "celine"
   portrait = "mad"
   local = "In this weather? Absolutely not."

# Requires-only overrides of the vanilla acceptance entries. Stating just
# the requires array replaces it while the section merge keeps vanilla's
# dialogue and writes, so this is the whole edit. Copy the entry's vanilla
# requires and add the weather clause. With every vanilla entry gated on
# pleasant weather, exactly one entry matches on a rainy day, the refusal.
[beach_accept_1]
   requires = [
      { npc = "celine" },
      { date = "beach" },
      { da_celine_beach = 0 },
      { celine_is_spouse = false },
      { weather = "pleasant" },
   ]
```

The example's clause matches storms too, because a storm still writes the `weather` fact as `"rainy"`. In Winter the same weather writes `"snowy"` instead, so the clause never matches there. See [Weather Conditions](../RECIPES.md#weather-conditions) for the three variants.

```gml
// The guard, keyed on your own fact. The fact name carries the date, so
// refusing the beach leaves this NPC's other dates alone.
function my_mod_date_begin(_ctx) {
    if (T2R.read(format("{NpcId}_refused_{}_date", _ctx.npc, date_to_string(_ctx.date))) == true) {
        return false; // cancel the date before start_date_cutscene
    }
    return undefined; // allow
}

// Register the guard.
mmapi_guard("date.begin", my_mod_date_begin);
```

After a refusal, no `date_history` entry is written and no cooldown begins, so the player can ask again. On a day your refusal entry no longer matches, the vanilla acceptance will play as if nothing happened.

## Engine Wiring

- Seam [`date_begin`](../seams/date_begin.md) dispatches from `gml/scripts/Dates.gml`, guarding the `start_date_cutscene(npc, date)` call inside `run_date`'s `play_conversation` callback.

## See Also

- [date.run](date.run.md) - Take over a date at `run_date`'s head, before the acceptance conversation.
- [date.cutscene](date.cutscene.md) - Swap which cutscene the date plays, after this guard has allowed it to begin.
- [dialogue.play_guard](dialogue.play_guard.md) - Decline the acceptance conversation itself before it plays.
