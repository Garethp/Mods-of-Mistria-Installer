# Seam: date_cutscene

Threads the date cutscene name through a filter before `MIST.run_scene`, and into the completion chain.

`date_cutscene` is a **text seam** (`anchor` + `replace`). It feeds [date.cutscene](../hooks/date.cutscene.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/Dates.gml` |
| **Locator** | text anchor from the `prop_sprite` blackboard insert through the completion chain's `ends_day` gate in `start_date_cutscene(npc, date)` |
| **Op** | text (filter dispatch) |
| **Feeds** | [`date.cutscene`](../hooks/date.cutscene.md) |
| **Value filtered** | the cutscene name about to run, defaulting to `data.cutscene` |
| **ctx built** | `{ npc: npc, date: date }` |
| **Marker** | `mmapi_date_cutscene_filter` |

## The Edit

The pristine site runs the date's cutscene with `MIST.run_scene(data.cutscene)` and later gates the reward chain on that scene's `ends_day` flag, read from `CUTSCENES` by the date's own cutscene name. The seamed site hoists the name into `__mmapi_date_scene`, filters it through `mmapi_apply_filters("date.cutscene", ...)` with `{ npc: npc, date: date }` as ctx, validates the result, runs the scene by the local, and binds the local as a fourth argument of the reward chain's callback so the gate reads the scene that actually played. The dispatch line carries the marker as a trailing comment.

The dispatch fails open twice over. The filter call sits under its own try/catch, and the returned name is probed against the loaded cutscene table (`load_cutscenes` asserts a mist script behind every entry, so one probe proves both halves). An unknown or throwing result falls back to the vanilla scene with a rate-limited warning under the `mmapi` label, so a broken handler costs the swap but never the date.

Threading the name as a chain argument follows the engine's own pattern. Chain callbacks are not closures, which is why the pristine code already passes `[npc, date, item]` explicitly. The companion seam [date_cutscene_chain_args](date_cutscene_chain_args.md) extends that argument list to match the added parameter. The two entries apply together or not at all, since staging fails closed across the catalog.

When no handler is registered, or every handler declines, `__mmapi_date_scene` equals `data.cutscene` and the seamed code is behavior-identical to pristine.

## See Also

- [date.cutscene](../hooks/date.cutscene.md) - This is the hook this seam dispatches.
- [date_cutscene_chain_args](date_cutscene_chain_args.md) - The companion edit that carries the chosen name into the reward gate.
- [date_begin](date_begin.md) - The guard dispatch that runs earlier, before `start_date_cutscene` is called.
