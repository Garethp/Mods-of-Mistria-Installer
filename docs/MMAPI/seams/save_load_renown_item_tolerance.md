# Engine Fix: save_load_renown_item_tolerance

Parser half of the renown tolerance pair. A pending museum donation of a since-removed custom item is dropped with a logged warn instead of the load aborting.

`save_load_renown_item_tolerance` is an **engine fix**, a hook-less edit. It dispatches nothing, and there is no handler to register. See [Seams](../SEAMS.md) and [save_load_renown_list_tolerance](save_load_renown_list_tolerance.md) for the caller half.

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Player/RenownUtils.gml` |
| **Locator** | text anchor on the `MuseumDonation` case of `deserialize_renown_entry` |
| **Op** | text (guarded resolve) |
| **Marker** | `mmapi_save_renown_item_tolerance` |

## The Edit

```gml
        case RenownEntryType.MuseumDonation:
            var __mmapi_donated = try_string_to_item_id(entry.item); // mmapi_save_renown_item_tolerance
            if (__mmapi_donated == undefined) {
                warn("MMAPI: save carried a pending renown entry for unknown item '{}' - dropped", entry.item);
                return undefined;
            }
            return RenownEntry.MuseumDonation(__mmapi_donated);
```

## Why

Donating an item to the museum queues a renown entry naming the item, and a save written before the entry was processed carries that name. The load resolves it with fatal `string_to_item_id`, one of the few fatal item lookups left in the pipeline, while the item flag arrays all use the `try_` variants. One pending donation of a removed custom item aborts the load natively.

The fix resolves through the tolerant variant and drops the entry with a warn when the name is unknown. The player loses one pending renown grant for an item that no longer exists. The Gold and Quest entry kinds are untouched.

Zero-registrant inert: a resolvable item takes the same path to the same constructed entry.
