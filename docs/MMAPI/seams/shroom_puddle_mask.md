# Engine Fix: shroom_puddle_mask

Corrects the acid puddle's damage-tarball collision mask, a beta-wiring fix.

`shroom_puddle_mask` is an **engine fix**, an anchored edit with no hook behind it. Nothing dispatches. The corrected line is the whole feature. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/objects/Combat/obj_hot_patch.gml` |
| **Locator** | text anchor: the damage-tarball builder chain (`.set_acid()` / `.set_mask_index(...)` / `.set_provenance(...)`) |
| **Feeds** | (no hook) |
| **Marker** | `mmapi_hot_patch_damage_mask` |

## The Edit

The anchor is the builder chain in `obj_hot_patch.gml` that constructs the acid puddle's damage tarball:

```gml
.set_acid()
.set_mask_index(self.mask_index)
.set_provenance(self.owner.monster_id, self.owner.stats_entry)
```

The replace changes one argument: the tarball's mask is set from `self.sprite_index` instead of `self.mask_index`, so the damage area follows the sprite the puddle is actually displaying rather than the instance's `mask_index`, the wiring the beta got wrong. The corrected line carries the `mmapi_hot_patch_damage_mask` marker as a trailing comment. Provenance and the acid flag are untouched.

## See Also

- [game_step_begin_installs](game_step_begin_installs.md) - This is the catalog's other engine fix, the mmapi lifecycle root.
- [monster_shroom_should_hide](monster_shroom_should_hide.md) - This is the shroom's other seam. It guards the hide behavior of the monster that lays these puddles.
