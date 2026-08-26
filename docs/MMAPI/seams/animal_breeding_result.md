# Seam: animal_breeding_result

Filters the offspring roll before the fetus is stored.

`animal_breeding_result` is a **text seam** (`anchor` + `replace`). It feeds [animal.breeding_result](../hooks/animal.breeding_result.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Ranching/Stable.gml` |
| **Locator** | text anchor on the breeding pair's base fetus push in `on_new_day()`, the full push struct |
| **Op** | text (`anchor` + `replace`) |
| **Feeds** | [`animal.breeding_result`](../hooks/animal.breeding_result.md) |
| **Value filtered** | the `roll_animal_breeding(female.kind, female, male)` result |
| **ctx built** | `{ female: female, male: male, stable: self, is_gemini: false }` |
| **Marker** | `mmapi_stable_run_breeding_result_filters` |

## The Edit

Pristine `on_new_day()` rolls the offspring inline inside the fetus push. The replacement hoists the roll into `__mmapi_animal_breeding_roll`, filters it through `mmapi_apply_filters("animal.breeding_result", ...)` with a ctx built from locals already in scope, and pushes the accepted result in the roll's place. The accepted result is the filter output when it is a struct, and the untouched roll otherwise, following the defensive style of [request_board_fetch_pool](request_board_fetch_pool.md). A site-level catch keeps the roll if ctx construction or dispatch fails.

The paired [animal_breeding_result_gemini](animal_breeding_result_gemini.md) seam makes the same edit at the GeminiSeason extra push a few lines below, with `is_gemini` true. The two anchors differ by indentation and the gemini anchor carries its perk stat line, and the deserialize path's push has a different body, so all three sites stay distinct.

With zero handlers the filter returns the roll unchanged, so the seam is behaviorally identical to pristine. The dispatch runs once per breeding pair per stable per day.

## See Also

- [animal.breeding_result](../hooks/animal.breeding_result.md) - This is the hook this seam dispatches.
- [animal_breeding_result_gemini](animal_breeding_result_gemini.md) - The same edit at the GeminiSeason extra push.
- [request_board_fetch_pool](request_board_fetch_pool.md) - The defensive struct handling this seam follows.
