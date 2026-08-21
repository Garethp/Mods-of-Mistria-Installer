# Hook: date.run

Take over a date the moment the player commits to it.

`date.run` is an **override** hook. Register a callback with `mmapi_override`. See [Hooks](../HOOKS.md) for how registration and dispatch work. This override is **claim-scoped**: many mods may register, but return `undefined` for the dates/partners you do not own. Any non-`undefined` return claims the date.

## Contract

Fires at the top of `run_date(date, npc)` (`Dates.gml`), before the `DateAcceptance` conversation is requested and played and before the NPC is posed. ctx is `{ date, npc }`: `date` is the `Date` enum id, `npc` is the `NpcId`.

Return a non-`undefined` value to replace `run_date` entirely. The acceptance conversation and NPC posing are skipped, and your value becomes `run_date`'s return (return `true` for a handled date).

Return `undefined` to defer to the engine's normal handling.

`run_date` is the single entry point the date-selection UI routes every date through (`await_popup(run_date, [date, npc])`), so this hook sees every player-initiated date.

| | |
| --- | --- |
| **Fires** | At the top of `run_date(date, npc)`, before the acceptance conversation and NPC posing. |
| **ctx** | `{ date, npc }`, the `Date` id and the `NpcId`. |
| **Kind contract** | Claim-scoped: many mods may register; return `undefined` for dates you do not own. The first non-`undefined` value replaces the engine's behavior. |

### The ctx parameter

- `ctx.date` - the `Date` enum id (index into `DATES`).
- `ctx.npc` - the `NpcId` of the date partner.

## Usage

```gml
// date.run is an OVERRIDE: return a value to replace the game's whole run_date;
// return undefined to let the game (or another mod) run the normal date flow.
function dateable_olric_run_date(_ctx) {
    // Skip the DateAcceptance conversation and start the cutscene immediately.
    // Gate on _ctx.npc to restrict this to one partner and leave vanilla
    // acceptance dialogue intact for everyone else.
    start_date_cutscene(_ctx.npc, _ctx.date);
    return true; // handled - the engine's run_date never runs
}

mmapi_override("date.run", dateable_olric_run_date);
```

Calling `start_date_cutscene(npc, date)` is the engine's own next step; it reads only its arguments and the game globals (`MIST` / `DATES` / `CALENDAR` / `ARI`), so it can be called directly from the handler.

## Engine Wiring

- Seam [`date_run`](../seams/date_run.md) dispatches from `gml/scripts/Dates.gml`, at the head of `run_date(date, npc)`, before the acceptance conversation.

## See Also

- [date.cooldown](date.cooldown.md) - Change the date cooldown that decides whether the Date option appears at all, upstream of this hook.
- [date.begin](date.begin.md) - Cancel an accepted date after its acceptance conversation, before the cutscene.
- [date.cutscene](date.cutscene.md) - Swap which cutscene the date plays while keeping the vanilla pipeline. A claimant that calls `start_date_cutscene` directly still flows through it.
- [dialogue.play_guard](dialogue.play_guard.md) - Veto or reshape the conversation `run_date` would otherwise play.
- [npc.heart_points](npc.heart_points.md) - Filter the heart points the completed date awards.
