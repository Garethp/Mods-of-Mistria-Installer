# Engine Fix: statue_hp_death_sweep

Adds the Living Griffin Statue's missing depleted-hp death check, the branch every other hp-driven monster carries.

`statue_hp_death_sweep` is an **engine fix**, an anchored edit with no hook behind it. Nothing dispatches. The added branch is the whole feature. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/objects/obj_monster_statue.gml` |
| **Locator** | text anchor: the tail of the statue's damage drain (the drain-side kill check and its closing braces) |
| **Feeds** | (no hook) |
| **Marker** | `mmapi_statue_hp_sweep` |

## The Edit

Every hp-driven monster drains its damage receiver behind an `if self.hit_points > 0` gate and pairs it with a depleted-hp death check, in one of two shapes. Group 1 (mite, bat, cat, enchantern, sap, tome) makes it the gate's else branch:

```gml
if self.hit_points > 0 {
    // drain the receiver; a drained kill transitions to Dying in here
} else if csi != MiteState.Dying {
    self.fsm.change_state(MiteState.Dying);
}
```

Group 2 (spirit, shroom, clod, rock_stack) hoists the same check above the drain:

```gml
if self.hit_points <= 0 {
    if self.fsm.current_state_id() != SpiritState.Dying {
        self.fsm.change_state(SpiritState.Dying);
    }
}
```

The statue alone has neither. Its only Dying transition is drain-side, inside the gate, so hp reaching zero outside a drained hit fails the gate every frame from then on. The statue in that state is never dead, never draining another tarball, and undamageable. The replace appends to the statue's tail, transforming it into the Group 1 shape its damage code already follows:

```gml
if self.hit_points > 0 {
    var took_any_damage = false;

    while true {
        var next_dmg = self.receiver.try_take_damage();
        // ... apply damage, numbers, knockback ...
    }

    if took_any_damage {
        // ... tumble state switch, patience ...
        if self.hit_points <= 0 {
            self.fsm.change_state(StatueState.Dying);
        }
    }
} else if csi != StatueState.Dying {
    self.fsm.change_state(StatueState.Dying); // mmapi_statue_hp_sweep
}
```

`csi` is the statue's own cached `fsm.current_state_id()`; the guard prevents re-entry after a drain-side kill. The added line carries the `mmapi_statue_hp_sweep` marker as a trailing comment. The Dying state is self-contained (`monster_death_poof` after `config.dying_frames`), so a sweep-entered death is indistinguishable from a hit death.

## See Also

- [monster_death](monster_death.md) - This seam observes the death routine this fix makes reachable for a depleted statue.
- [monster_step_begin](monster_step_begin.md) - This is the per-frame seam a mod would use to observe (or deplete) monster hp; the fix makes an hp write lethal for the statue the way it already is for every swept monster.
- [game_step_begin_installs](game_step_begin_installs.md) - This is another of the catalog's engine fixes, the MMAPI lifecycle root.
- [shroom_puddle_mask](shroom_puddle_mask.md) - This is another of the catalog's engine fixes, also a combat-object correction.
