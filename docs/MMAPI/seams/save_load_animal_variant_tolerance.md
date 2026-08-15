# Engine Fix: save_load_animal_variant_tolerance

Lets a save load and play when a barn animal wears a since-removed custom variant. The animal falls back to its kind's first variant with a logged warn.

`save_load_animal_variant_tolerance` is an **engine fix**, a hook-less edit. It dispatches nothing, and there is no handler to register. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Ranching/Animal.gml` |
| **Locator** | text anchor on the `variant` line of `deserialize` |
| **Op** | text (guarded normalize) |
| **Marker** | `mmapi_save_animal_variant_tolerance` |

## The Edit

```gml
        self.variant = animal_data.variant; // mmapi_save_animal_variant_tolerance
        if (self.prototype.variants.contains_key(self.variant) == false) {
            var __mmapi_av_keys = self.prototype.variants.keys();
            warn("MMAPI: save carried animal variant '{}' that no longer exists - replaced with '{}'", self.variant, __mmapi_av_keys[0]);
            self.variant = __mmapi_av_keys[0];
        }
```

## Why

A saved barn animal's variant is copied raw at load and only resolved later, when gameplay dereferences `prototype.variants.get(variant)` for production tier or sprites. The pet has an explicit fallback for exactly this case, and barn animals had none, so a variant from a removed mod loaded fine and then crashed in play. Variants load from fiddle content, so mods can mint them.

The fix normalizes at deserialize time: an unknown variant becomes the kind's first variant, named in the warn. The animal keeps its name, hearts, and produce state.

Zero-registrant inert: every vanilla variant is in its kind's prototype map, so the guard never fires on an intact install.
