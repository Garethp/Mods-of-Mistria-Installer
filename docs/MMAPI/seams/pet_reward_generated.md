# Seams: pet_reward_generated

Emits an event for each concrete scheduled pet reward item.

`pet_reward_generated` is provided by two **text seams**. Both feed [pet.reward_generated](../hooks/pet.reward_generated.md). Mod authors register handlers for the hook; they do not write seams. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/Pet.gml` |
| **Locators** | the forageable reward append and the fixed-item reward append inside `pet_update_at_time()` |
| **Op** | text insertion |
| **Feeds** | [`pet.reward_generated`](../hooks/pet.reward_generated.md) |
| **Context** | `{ pet: PET, job: PET.job, item }` |
| **Markers** | `mmapi_pet_run_forageable_reward_callbacks`, `mmapi_pet_run_item_reward_callbacks` |

## Behavior

The forageable seam emits only after a valid forageable is appended. The fixed-item seam emits after each fixed reward item is appended, so a multi-item reward produces one event per item. No scheduler entry hook is added, and no event fires when the pet has no active job or no forageable was rolled.
