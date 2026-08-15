# Engine Fix: save_load_mount_variant_tolerance

Lets a save load and play when the mount wears a since-removed custom variant. The mount falls back to its kind's first variant with a logged warn.

`save_load_mount_variant_tolerance` is an **engine fix**, a hook-less edit. It dispatches nothing, and there is no handler to register. See [Seams](../SEAMS.md) and [save_load_animal_variant_tolerance](save_load_animal_variant_tolerance.md) for the barn-animal counterpart.

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Cycle/LoadGame.gml` |
| **Locator** | text anchor on the mount's prototype assignment |
| **Op** | text (guarded normalize) |
| **Marker** | `mmapi_save_mount_variant_tolerance` |

## The Edit

```gml
        ARI.mount.prototype = ANIMAL_PROTOTYPES[ARI.mount.kind];
        if (ARI.mount.prototype.variants.contains_key(ARI.mount.variant) == false) { // mmapi_save_mount_variant_tolerance
            var __mmapi_mv_keys = ARI.mount.prototype.variants.keys();
            warn("MMAPI: save carried mount variant '{}' that no longer exists - replaced with '{}'", ARI.mount.variant, __mmapi_mv_keys[0]);
            ARI.mount.variant = __mmapi_mv_keys[0];
        }
```

## Why

The load sets the mount's variant from the save several lines before it assigns the prototype, so the guard anchors on the prototype assignment and normalizes immediately after it. Without the guard, a mount variant from a removed mod survives the load and crashes at the first `prototype.variants.get` dereference in play, the same hazard the barn-animal fix closes.

Zero-registrant inert: a vanilla mount variant is always in the prototype map, so the guard never fires on an intact install.
