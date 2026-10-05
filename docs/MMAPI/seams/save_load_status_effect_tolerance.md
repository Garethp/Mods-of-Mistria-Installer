# Engine Fix: save_load_status_effect_tolerance

A status-effect slot whose type no longer resolves is dropped with a logged warn instead of aborting the load.

`save_load_status_effect_tolerance` is an **engine fix**, an anchored edit with no hook behind it. Nothing dispatches. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/Player/StatusEffectManager.gml` |
| **Locator** | text anchor on the slot restore in `deserialize` |
| **Feeds** | (no hook) |
| **Marker** | `mmapi_save_status_tolerance` |

## The Edit

The replace guards the conversion with the tolerant variant.

```gml
            self.effects.set(i, opt_and_then(effect, function(e) {
                var __mmapi_se_type = try_string_to_status_effect_id(e.type); // mmapi_save_status_tolerance
                if (__mmapi_se_type == undefined) {
                    warn("MMAPI: save carried unknown status effect '{}' - dropped", e.type);
                    return undefined;
                }
                e.type = __mmapi_se_type;
                return e;
            }));
```

Active status effects persist inside the player blob as an ordinal-indexed slot array whose entries carry their type by name, restored through the fatal `string_to_status_effect_id`. It is one of the fatal name lookups the `save_load_*` family covers, with [save_load_spells_tolerance](save_load_spells_tolerance.md) as its closest sibling. A save written while a custom effect was active aborts silently on a clean install.

The guarded conversion uses the `try_` variant, which returns `undefined` for unknown names. Returning `undefined` from the `opt_and_then` callback is exactly what an empty slot deserializes to, so the manager's update loop never sees the dropped entry.

A registered [status_effect](../extensions/status_effect.md) normally survives its mod's removal through the ledger's tombstone, because the vacant enum member keeps the name resolving and the effect ticks out inert. This guard covers the remaining cases, a hand-wiped ledger or a save moved to an install with a different ledger history.

On an intact install every type resolves and restores identically, and expired effects leave empty slots that were never at risk.

## See Also

- [status_effect](../extensions/status_effect.md) - The extension point whose vacancy this fix backstops.
- [Custom Status Effects](../CUSTOM_STATUS_EFFECTS.md) - The guide, with the uninstall story in full.
- [save_load_spells_tolerance](save_load_spells_tolerance.md) - The sibling fix for learned spells.
