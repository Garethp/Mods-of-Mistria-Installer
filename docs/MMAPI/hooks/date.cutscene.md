# Hook: date.cutscene

Swap which cutscene a date plays while keeping the whole vanilla date pipeline.

`date.cutscene` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires in `start_date_cutscene(npc, date)` (`Dates.gml`), after the date blackboard writes and the prop selection and immediately before `MIST.run_scene` starts the date cutscene. The filtered value is the cutscene name about to run, defaulting to `DATES[date].cutscene`. ctx is `{ npc, date }`: `npc` is the `NpcId`, `date` is the `Date` enum id. Return a replacement cutscene name to swap which scene plays, or `undefined` to keep the current value.

A replacement must name a loaded cutscene, meaning a fiddle `cutscenes.toml` entry backed by a mist script. Ship both with your mod, cloned verbatim from the vanilla scene's entries minus identity. An unknown name is dropped with a rate-limited warning and the vanilla scene runs.

The chosen name is threaded through the date's completion chain, and the reward gate honours the replacement scene's own `ends_day` flag. Your replacement's `cutscenes.toml` entry is the single source of truth for whether the date ends the day, exactly as the engine already treats it for ending the day and for item delivery.

A replacement that ends the day grants its rewards over the end-of-day menu the way vanilla does, and a replacement that does not end the day grants them immediately at scene end the way vanilla does. The rest of the completion pipeline (heart points, the date photo, the reward popup, `date_history`) is keyed by `npc` and `date` and runs unchanged.

| | |
| --- | --- |
| **Fires** | Inside `start_date_cutscene`, immediately before `MIST.run_scene`, after the blackboard and prop selection. |
| **Value** | The cutscene name about to run, defaulting to `DATES[date].cutscene`. |
| **ctx** | `{ npc, date }` - the `NpcId` and the `Date` enum id. |
| **Kind contract** | Chained filter: return a replacement name, or `undefined` to keep the current value. Later filters receive the current result. |

### The ctx struct

- `npc` - the `NpcId` of the date partner.
- `date` - the `Date` enum id (index into `DATES`).

## Usage

```gml
// date.cutscene is a FILTER: return a cutscene name to swap which scene the
// date plays, or undefined to keep the current value (the vanilla scene, or
// another mod's earlier replacement).
function custom_dates_date_cutscene(_value, _ctx) {
    // Gate on your own T2 fact so the swap only fires for dates you own.
    // date_to_string(_ctx.date) is the engine's own name for the date (the
    // fiddle key, for example "bathhouse"), so one handler serves every date.
    if (T2R.read(format("{NpcId}_custom_date", _ctx.npc)) == true) {
        return format("{NpcId}_custom_{}_date", _ctx.npc, date_to_string(_ctx.date));
    }
    return undefined; // not ours - keep the current value
}

// Inside your latched register function (see Mod Anatomy):
mmapi_filter("date.cutscene", custom_dates_date_cutscene);
```

The `{NpcId}` format placeholder stringifies the NPC enum by name, the same way the engine uses when it writes a T2 fact for each NPC. Since the `{Date}` placeholder is claimed for localizing calendar dates, use `date_to_string(date)` with a plain `{}` placeholder for the date's name instead.

## Engine Wiring

- Seam [`date_cutscene`](../seams/date_cutscene.md) dispatches from `gml/scripts/Dates.gml`, immediately before `MIST.run_scene` in `start_date_cutscene(npc, date)`, and threads the chosen name into the completion chain.
- Seam [`date_cutscene_chain_args`](../seams/date_cutscene_chain_args.md) is the companion edit that extends the completion chain's argument list so the chosen name reaches the reward gate.

## See Also

- [date.run](date.run.md) - Take over a date at `run_date`'s head, before the acceptance conversation. A `date.run` claimant that calls `start_date_cutscene` directly still flows through this filter.
- [date.begin](date.begin.md) - Cancel an accepted date after its acceptance conversation, before this filter fires.
- [npc.heart_points](npc.heart_points.md) - Filter the heart points the completed date awards.
