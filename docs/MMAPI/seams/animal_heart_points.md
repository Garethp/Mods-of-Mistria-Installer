# Seam: animal_heart_points

Reroutes every barn-animal heart-point delta through a filter before it lands.

`animal_heart_points` is a **text seam** (filter-shaped: it dispatches `mmapi_apply_filters`). It feeds [animal.heart_points](../hooks/animal.heart_points.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Ranching/Animal.gml` |
| **Locator** | text anchor at the head of `Animal.add_heart_points(points)`, right after the `static MAX_POINTS` line |
| **Op** | text (filter dispatch) |
| **Feeds** | [`animal.heart_points`](../hooks/animal.heart_points.md) |
| **Value filtered** | `points` - the heart points delta |
| **ctx built** | `self` (the `Animal` struct) |
| **Marker** | `mmapi_animal_run_heart_filters` |

## The Edit

The seam re-states the head of `add_heart_points(points)` (the signature plus its `static MAX_POINTS = animal_heart_level_to_points(10);` line) and appends one injected line:

```gml
points = mmapi_apply_filters("animal.heart_points", points, self); // mmapi_animal_run_heart_filters
```

The reassignment runs before the delta is applied, so whatever a filter returns is what the animal's heart total actually absorbs. ctx is `self`, the `Animal` struct, letting a handler scope its change to specific animals. The injected line is a bare reassignment with no try/catch of its own. Per-handler isolation lives inside the registry dispatch, and with zero handlers `mmapi_apply_filters` hands `points` back unchanged, leaving pristine behavior.

## See Also

- [animal.heart_points](../hooks/animal.heart_points.md) - This is the hook that this seam dispatches.
- [npc_heart_points](npc_heart_points.md) - This is the same edit on villager heart points in `Npc.add_heart_points()`.
- [animal_on_pet](animal_on_pet.md) - This is the event emit when the player pets an animal.
