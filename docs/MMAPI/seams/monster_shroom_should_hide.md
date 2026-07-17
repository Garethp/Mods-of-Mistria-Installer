# Seam: monster_shroom_should_hide

Puts a veto check at the head of the shroom's hide decision.

`monster_shroom_should_hide` is a **template seam** (`op = "guard"`). It feeds [monster.shroom.should_hide](../hooks/monster.shroom.should_hide.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/objects/Combat/obj_monster_shroom.gml` |
| **Locator** | structural target: `should_hide`, at head |
| **Op** | `guard` |
| **Feeds** | [`monster.shroom.should_hide`](../hooks/monster.shroom.should_hide.md) |
| **ctx built** | `self` - the shroom instance |
| **On veto** | `return false;` |
| **Marker** | `shroom_should_hide` |

## The Edit

The generated dispatch lands at the head of the shroom monster's `should_hide()` check, before its normal distance test. It calls `mmapi_check_guards("monster.shroom.should_hide", self)` in the uniform try/catch shape. When any guard returns `false`, the injected line runs `return false;`. `should_hide` answers no and the shroom stays out in the open. `undefined` or `true` falls through to the pristine distance check, so a deferring handler changes nothing.

The locator is structural (function + position + token anchor, matched token-wise), so it is immune to whitespace and comment drift inside `obj_monster_shroom.gml`. With zero handlers the seam is behaviorally identical to pristine.

## See Also

- [monster.shroom.should_hide](../hooks/monster.shroom.should_hide.md) - This is the hook that this seam dispatches.
- [monster_spirit_projectile_step](monster_spirit_projectile_step.md) - This seam is the other monster-behavior guard.
- [shroom_puddle_mask](shroom_puddle_mask.md) - This is the hook-less engine fix for the shroom's acid puddle collision mask.
