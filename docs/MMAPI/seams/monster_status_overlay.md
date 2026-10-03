# Engine Fix: monster_status_overlay

Sets the flat draw kind on the monster status overlay and defaults status particles on, matching the hit flash `setup_white_vfx` builds the same way.

`monster_status_overlay` is an **engine fix**, an anchored edit with no hook behind it. Nothing dispatches. The two restored settings are the whole feature. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/objects/Combat/par_monster.gml` |
| **Locator** | text anchor: the three lines in `create()` that build `status_blender` and seed `status_cap` and `status_particles` |
| **Feeds** | (no hook) |
| **Marker** | `mmapi_monster_status_overlay` |

## The Edit

The anchor is the status setup at the end of `par_monster`'s overlay color block in `create()`:

```gml
self.status_blender = self.create_renderable();
self.status_cap = infinity;
self.status_particles = false;
```

The replace adds one line and changes one value. `self.status_blender.draw_kind = DrawKind.Flat;` follows the renderable's creation, and `self.status_particles` becomes `true`. Both carry the `mmapi_monster_status_overlay` marker as a trailing comment.

`par_monster.step_end` drives the `status_blender` renderable every frame. When a monster carries the Venomous or Frozen status, the blender takes the monster's sprite, frame, position and flip, the overlay color as `tint`, and the overlay alpha. The colors and alphas come from `mines_overlay_values` in `misc.toml`, green at 0.39 for venom and pale blue at 0.29 for frozen. Otherwise the blender's animation is cleared. `create()` builds the blender with a bare `create_renderable()` and never sets a draw kind, so it draws in the Normal kind, where `tint` multiplies into the sprite. The sprite multiplied by pale blue at 0.29 alpha, drawn over the sprite itself, is not a visible overlay.

The same `step_end` makes the one `process_status(self.status_cap, self.status_particles)` call for every monster. Its particle branch spawns the venom damage number and `spr_fx_monster_venom_damage` for a venomous monster, or `spr_fx_monster_ice_puff` for a frozen one, every 60 ticks. `status_particles` is seeded `false` in `create()`, and only the mite writes it, true while hurt or dying, so no other monster spawns status particles. The hit flash built one function over in `setup_white_vfx` sets `white_vfx.draw_kind = DrawKind.Flat` on a renderable it drives the same way. The status mechanics are unaffected either way. Venom ticks its damage every 60 ticks and Frozen scales movement.

With the fix staged, the blender draws in the Flat kind, a silhouette of the sprite in the overlay color at the overlay alpha, and status particles spawn for every monster. The mite still writes `status_particles` per state after the default. The clod's `status_cap` of 1 is untouched.

## See Also

- [monster_step_end](monster_step_end.md) - The seam at the end of the same `step_end`, after the overlay is driven.
- [game_step_begin_installs](game_step_begin_installs.md) - This is another of the catalog's engine fixes, the MMAPI lifecycle root.
- [Renderables](../RENDERABLES.md) - The renderable fields a mod drives on its own renderables.
