# Engine Fix: save_load_pet_items_tolerance

Lets a save load when the pet's queued drop items include a since-removed custom item. Unknown items are dropped from the queue with a logged warn instead of the load aborting.

`save_load_pet_items_tolerance` is an **engine fix**, a hook-less edit. It dispatches nothing, and there is no handler to register. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/Pet.gml` |
| **Locator** | text anchor on the `items_to_pop` deserialize |
| **Op** | text (filter loop) |
| **Marker** | `mmapi_save_pet_items_tolerance` |

## The Edit

```gml
        self.items_to_pop = []; // mmapi_save_pet_items_tolerance
        for (var __mmapi_pi = 0; __mmapi_pi < array_length(pet_data.items_to_pop); __mmapi_pi++) {
            var __mmapi_pit = try_string_to_item_id(pet_data.items_to_pop[__mmapi_pi]);
            if (__mmapi_pit == undefined) {
                warn("MMAPI: save carried an unknown item '{}' queued on the pet - dropped", pet_data.items_to_pop[__mmapi_pi]);
                continue;
            }
            array_push(self.items_to_pop, __mmapi_pit);
        }
```

## Why

The pet's pending item drops are stored by name and resolved with fatal `string_to_item_id`, while the item flag arrays elsewhere in the load use the tolerant `try_` variants. A custom item sitting in the pet's queue when its mod is removed aborts the load natively.

The fix filters the queue through the tolerant variant with a warn per dropped item. The pet delivers the rest of its queue as normal.

Zero-registrant inert: resolvable items produce the same queue in the same order as the vanilla `array_map`.
