# Engine Fix: save_load_mount_variant_tolerance

Lets a save load and play when the mount wears a since-removed custom variant. The mount falls back to its kind's first variant with a logged warn.

`save_load_mount_variant_tolerance` is an **engine fix**, an anchored edit with no hook behind it. Nothing dispatches. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Cycle/LoadGame.gml` |
| **Locator** | text anchor on the mount's prototype assignment |
| **Feeds** | (no hook) |
| **Marker** | `mmapi_save_mount_variant_tolerance` |

## The Edit

The replace normalizes an unknown variant right after the prototype assignment.

```gml
        ARI.mount.prototype = ANIMAL_PROTOTYPES[ARI.mount.kind];
        if (ARI.mount.prototype.variants.contains_key(ARI.mount.variant) == false) { // mmapi_save_mount_variant_tolerance
            var __mmapi_mv_keys = ARI.mount.prototype.variants.keys();
            warn("MMAPI: save carried mount variant '{}' that no longer exists - replaced with '{}'", ARI.mount.variant, __mmapi_mv_keys[0]);
            ARI.mount.variant = __mmapi_mv_keys[0];
        }
```

The load sets the mount's variant from the save several lines before it assigns the prototype, so the guard anchors on the prototype assignment and normalizes immediately after it. Without the guard, a mount variant from a removed mod survives the load and crashes at the first `prototype.variants.get` dereference during gameplay, the same hazard the animal fix closes.

A vanilla mount variant is always in the prototype map, so the guard never fires on an intact install.

## See Also

- [save_load_animal_variant_tolerance](save_load_animal_variant_tolerance.md) - The twin of this fix for barn/coop animals.
