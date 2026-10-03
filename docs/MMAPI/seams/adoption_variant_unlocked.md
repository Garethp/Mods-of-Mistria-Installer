# Seam: adoption_variant_unlocked

Filters the adoption menu's variant unlock read for each variant row.

`adoption_variant_unlocked` is a **text seam** (`anchor` + `replace`). It feeds [animal.adoption_variant_unlocked](../hooks/animal.adoption_variant_unlocked.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/UI/Anchor/Menus/AdoptionMenu.gml` |
| **Locator** | text anchor on the `var unlocked` read in `setup_right_page()`'s variant loop |
| **Op** | text (`anchor` + `replace`) |
| **Feeds** | [`animal.adoption_variant_unlocked`](../hooks/animal.adoption_variant_unlocked.md) |
| **Value filtered** | the `unlocked` boolean from `ARI.animal_variant_unlocks` |
| **ctx built** | `{ animal_kind: animal_kind, variant: variant, has_gold: has_gold, has_room: has_room }` |
| **Marker** | `mmapi_adoption_run_variant_unlocked_filters` |

## The Edit

Pristine `setup_right_page()` reads the save's unlock state into one local, and every gate on the row derives from it. The name reveal, the icon blackout, the entry fade, and the tap callback all read `unlocked`. The replacement keeps the pristine read and filters the local immediately after it, so all four consumers see one coherent answer. A site-level catch keeps the save's value if ctx construction or dispatch fails.

The `has_gold` and `has_room` locals are computed just above the read, ride along in ctx, and keep gating the tap callback on their own. A filtered `true` therefore shows and prices the variant while the engine still refuses a purchase the player cannot afford or house. The purchase path is untouched, so buying writes the real unlock through `initialize_new_animal_for_ari()`.

With zero handlers the filter returns the save's value unchanged, so the seam is behaviorally identical to pristine. The dispatch runs once per acquirable variant row on every right page build, and the page rebuilds after each purchase.

## See Also

- [animal.adoption_variant_unlocked](../hooks/animal.adoption_variant_unlocked.md) - This is the hook this seam dispatches.
- [animal_breeding_result](animal_breeding_result.md) - Breeding rolls are the other way new variants reach the player.
