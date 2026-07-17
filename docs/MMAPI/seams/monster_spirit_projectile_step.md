# Seam: monster_spirit_projectile_step

Puts a destroy-on-veto check into the spirit projectile's step.

`monster_spirit_projectile_step` is a **template seam** (`op = "guard"`). It feeds [monster.spirit_projectile.step](../hooks/monster.spirit_projectile.step.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/objects/Combat/obj_monster_spirit_projectile.gml` |
| **Locator** | pristine context: in the step, after the block that starts the projectile's loop sound (`SoundEffects/Enemies/FlameSprite/ProjectileLoop`), before `if self.image_alpha < 1 {` |
| **Op** | `guard` |
| **Feeds** | [`monster.spirit_projectile.step`](../hooks/monster.spirit_projectile.step.md) |
| **ctx built** | `self` - the projectile instance |
| **On veto** | `instance_destroy(); return;` |
| **Marker** | `spirit_projectile_step` |

## The Edit

The generated dispatch lands in the spirit projectile's step, after the block that starts its looping flight sound and before the fade-in logic. It calls `mmapi_check_guards("monster.spirit_projectile.step", self)` in the uniform try/catch shape. When any guard returns `false`, the injected line runs `instance_destroy(); return;`. The projectile is destroyed on the spot and the rest of that step never runs. `undefined` or `true` lets the step proceed normally.

The guard fires every step of every live spirit projectile, so a handler can watch flight (position, alpha, target) each frame and pull the plug the moment its condition trips. With zero handlers the seam is behaviorally identical to pristine.

## See Also

- [monster.spirit_projectile.step](../hooks/monster.spirit_projectile.step.md) - This is the hook that this seam dispatches.
- [monster_shroom_should_hide](monster_shroom_should_hide.md) - This seam is the other monster-behavior guard.
