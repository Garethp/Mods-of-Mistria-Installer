# Seam: animal_breeding_result_gemini

Filters the extra GeminiSeason offspring roll at its own push site.

`animal_breeding_result_gemini` is a **text seam** (`anchor` + `replace`). It feeds [animal.breeding_result](../hooks/animal.breeding_result.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Ranching/Stable.gml` |
| **Locator** | text anchor on the GeminiSeason extra fetus push in `on_new_day()`, including its perk stat line |
| **Op** | text (`anchor` + `replace`) |
| **Feeds** | [`animal.breeding_result`](../hooks/animal.breeding_result.md) |
| **Value filtered** | the `roll_animal_breeding(female.kind, female, male)` result |
| **ctx built** | `{ female: female, male: male, stable: self, is_gemini: true }` |
| **Marker** | `mmapi_stable_run_breeding_result_gemini_filters` |

## The Edit

The GeminiSeason perk sometimes grants a breeding pair a second fetus, and pristine `on_new_day()` rolls it inline inside a second push. The replacement mirrors [animal_breeding_result](animal_breeding_result.md) with its own hoisted locals: the roll is filtered with `is_gemini` true, a struct result replaces it, and anything else keeps the roll. The perk stat line is carried through unchanged.

The engine only reaches this push when the perk roll passes and space checks succeed, so the hook observes the extra fetus rather than deciding whether it exists. With zero handlers the seam is behaviorally identical to pristine.

## See Also

- [animal.breeding_result](../hooks/animal.breeding_result.md) - This is the hook this seam dispatches.
- [animal_breeding_result](animal_breeding_result.md) - The base push site this seam mirrors.
