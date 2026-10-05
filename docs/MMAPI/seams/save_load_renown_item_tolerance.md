# Engine Fix: save_load_renown_item_tolerance

Parser half of the renown tolerance pair. A pending museum donation of a since-removed custom item is dropped with a logged warn instead of the load aborting.

`save_load_renown_item_tolerance` is an **engine fix**, an anchored edit with no hook behind it. Nothing dispatches. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Player/RenownUtils.gml` |
| **Locator** | text anchor on the `MuseumDonation` case of `deserialize_renown_entry` |
| **Feeds** | (no hook) |
| **Marker** | `mmapi_save_renown_item_tolerance` |

## The Edit

The replace resolves the item through the tolerant variant and drops the entry when the name is unknown.

```gml
        case RenownEntryType.MuseumDonation:
            var __mmapi_donated = try_string_to_item_id(entry.item); // mmapi_save_renown_item_tolerance
            if (__mmapi_donated == undefined) {
                warn("MMAPI: save carried a pending renown entry for unknown item '{}' - dropped", entry.item);
                return undefined;
            }
            return RenownEntry.MuseumDonation(__mmapi_donated);
```

Donating an item to the museum queues a renown entry naming the item, and a save written before the entry was processed carries that name. The load resolves it with the fatal `string_to_item_id`, one of the few item lookups in the pipeline that is not tolerant, while the item flag arrays all use the `try_` variants. One pending donation of a removed custom item aborts the load natively.

The fix resolves through the tolerant variant and drops the entry with a warn when the name is unknown. The player loses one pending renown grant for an item that no longer exists. The Gold and Quest entry kinds are untouched.

A resolvable item takes the same path to the same constructed entry, so an intact install is unchanged.

## See Also

- [save_load_renown_list_tolerance](save_load_renown_list_tolerance.md) - The caller half of the pair.
- [save_load_infusion_tolerance](save_load_infusion_tolerance.md) - Another fatal item-side lookup the family covers.
