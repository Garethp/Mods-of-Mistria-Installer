# Hook: animal.adoption_variant_unlocked

Change which animal variants the adoption menu offers.

`animal.adoption_variant_unlocked` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires in `AdoptionMenu.setup_right_page()`, once per acquirable variant row, as the menu reads `ARI.animal_variant_unlocks` for that variant. The filtered value is the `unlocked` boolean, and it gates the variant's name reveal, icon blackout, entry fade, and buyability together. ctx is `{ animal_kind, variant, has_gold, has_room }`. Return a replacement boolean, or `undefined` to keep the save's unlock state.

A final `true` offers the variant without writing anything to the save, and buying still flows through `initialize_new_animal_for_ari()`, which records the real unlock. A final `false` hides a variant the save has unlocked. Locked species never reach this page, so the filter cannot unlock a species early. Variants flagged not acquirable are filtered out earlier and never dispatch.

| | |
| --- | --- |
| **Fires** | In `AdoptionMenu.setup_right_page()`, once per acquirable variant row, on every page build. |
| **Value** | The `unlocked` boolean, read from `ARI.animal_variant_unlocks`. |
| **ctx** | `{ animal_kind, variant, has_gold, has_room }` |
| **Kind contract** | The callback receives the current value and returns a replacement, or `undefined` to keep the current value. |

### The ctx struct

- `animal_kind` - the `AnimalKind` the right page shows.
- `variant` - the variant data struct. Read `variant.key` for the unlock key and `variant.tier` for its tier.
- `has_gold` - whether the player can afford this variant. The engine still enforces it for buying.
- `has_room` - whether a matching building has a free stall. The engine still enforces it for buying.

## Usage

```gml
// animal.adoption_variant_unlocked is a FILTER: you receive (value, ctx) and
// return a replacement, or undefined to keep the game's value.
function full_palette_animal_adoption_variant_unlocked(_value, _ctx) {
    // _value is the save's unlock state for this variant row.
    // _ctx is { animal_kind, variant, has_gold, has_room }.
    // e.g. offer every acquirable color of the species on show:
    // return true;
    return undefined; // undefined = keep the save's unlock state
}

// inside your latched register function (see Mod Anatomy):
mmapi_filter("animal.adoption_variant_unlocked", full_palette_animal_adoption_variant_unlocked);
```

## Interactions

- The hook is scoped to this menu's read. Other readers of `ARI.animal_variant_unlocks`, like the giant chicken checks and the mount variant list, still see the real save state.
- Buying writes the real unlock through `initialize_new_animal_for_ari()`, the same path that records a hatched offspring's variant.
- The page rebuilds after each purchase and on each species selection, so the hook fires again with fresh `has_gold` and `has_room` gates.

## Engine Wiring

- Seam [`adoption_variant_unlocked`](../seams/adoption_variant_unlocked.md) dispatches from `gml/scripts/UI/Anchor/Menus/AdoptionMenu.gml`, filtering the `unlocked` local right after the engine reads it.

## See Also

- [animal.breeding_result](animal.breeding_result.md) - Breeding is the other way variants reach the player.
- [ui.menu_opened](ui.menu_opened.md) - Know when a menu, this one included, opens.
