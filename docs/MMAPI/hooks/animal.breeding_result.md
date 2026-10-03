# Hook: animal.breeding_result

Change the offspring a breeding pair rolls.

`animal.breeding_result` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires in `Stable.on_new_day()` for each breeding pair, after the engine rolls the offspring and before the fetus is stored in `incubating_fetuses`. The filtered value is the rolled offspring struct `{ kind, variant, sex, name }`. ctx is `{ female, male, stable, is_gemini }`. Two seams share this hook, one per push site, and `is_gemini` tells them apart.

Return a replacement struct, mutate the given struct in place, or return `undefined` to keep the roll. A non-struct return is dropped and the roll stands.

| | |
| --- | --- |
| **Fires** | In `Stable.on_new_day()`, once per fetus a breeding pair creates, before the push into `incubating_fetuses`. |
| **Value** | The rolled offspring struct, `{ kind, variant, sex, name }`. |
| **ctx** | `{ female, male, stable, is_gemini }` |
| **Kind contract** | The callback receives the current value and returns a replacement, or `undefined` to keep the current value. |

### The value struct

- `kind` - the offspring `AnimalKind`. Vanilla always passes the mother's kind. Changing it bypasses checks the engine ran before the roll. Incubator space was tested against the mother's species, and `on_room_start()` pairs egg fetuses to incubator instances by count, so a kind swap into an egg species can exceed the room's incubators.
- `variant` - the offspring variant key, rolled from the variants of the tier that are acquirable and born in the current season.
- `sex` - `Sex.Female` or `Sex.Male`.
- `name` - the rolled name. It is only a starting value, because the player renames the newborn at the hatching popup.

### The ctx struct

- `female`, `male` - the parent `Animal` structs.
- `stable` - the `Stable` processing its new day (`self` inside `on_new_day()`).
- `is_gemini` - `true` for the extra fetus the GeminiSeason perk adds.

## Usage

```gml
// animal.breeding_result is a FILTER: you receive (value, ctx) and return a
// replacement, or undefined to keep the game's value.
function painted_flock_animal_breeding_result(_value, _ctx) {
    // _value is the rolled offspring: { kind, variant, sex, name }.
    // _ctx is { female, male, stable, is_gemini }.
    // e.g. always roll female offspring:
    // _value.sex = Sex.Female;
    // _value.name = random_animal_name(_value.sex);
    return undefined; // undefined = keep the (possibly mutated) roll
}

// inside your latched register function (see Mod Anatomy):
mmapi_filter("animal.breeding_result", painted_flock_animal_breeding_result);
```

## Interactions

- Stables process before [game.new_day](game.new_day.md) fires, and the end-of-day autosave then captures the filtered fetus.
- A hatched offspring's variant becomes a real unlock through `initialize_new_animal_for_ari()`, the same path the adoption menu's purchases use.
- The engine's debug menu calls `roll_animal_breeding()` directly, and those rolls never dispatch this hook. The seams sit at the fetus push sites, not around the roll function.

## Engine Wiring

- Seam [`animal_breeding_result`](../seams/animal_breeding_result.md) dispatches from `gml/scripts/GameplaySystems/Ranching/Stable.gml`, at the breeding pair's base fetus push, with `is_gemini` false.
- Seam [`animal_breeding_result_gemini`](../seams/animal_breeding_result_gemini.md) dispatches from the same function's GeminiSeason extra push, with `is_gemini` true.

## See Also

- [animal.adoption_variant_unlocked](animal.adoption_variant_unlocked.md) - The adoption menu is the other way variants reach the player.
- [animal.production_gate](animal.production_gate.md) - Change which animals are eligible to produce each day.
- [game.new_day](game.new_day.md) - The day-boundary event that fires after all stables have processed.
