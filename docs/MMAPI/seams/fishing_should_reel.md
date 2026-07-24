# Seam: fishing_should_reel

Filters the fishing Wait state's reel decision before the complete vanilla reel block.

`fishing_should_reel` is a **text seam** (`anchor` + `replace`). It feeds [fishing.should_reel](../hooks/fishing.should_reel.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/Player/AriFsm.gml` |
| **Locator** | text anchor on the reel-input condition inside `FishingState.Wait`, including the following `fishing_sfx` line |
| **Op** | text (`anchor` + `replace`) |
| **Feeds** | [`fishing.should_reel`](../hooks/fishing.should_reel.md) |
| **Value filtered** | the boolean OR of `UseToolCharged` and `UseToolRepeated` |
| **ctx built** | `{ bite_active: FISHING.bite_timer > 0 }` |
| **Marker** | `mmapi_fishing_run_should_reel_filters` |

## The Edit

Pristine `AriFsm.gml` uses the same tool-input expression in both `FishingState.Windup` and `FishingState.Wait`. The locator includes Wait's following `fishing_sfx` assignment so the anchor identifies exactly one site.

The replacement evaluates the pristine input expression once into `__mmapi_fishing_should_reel`, filters that boolean through `mmapi_apply_filters("fishing.should_reel", ..., { bite_active: FISHING.bite_timer > 0 })`, then uses the final value as the original `if` condition. A site-level catch leaves the captured input result unchanged if context construction, dispatch, or assignment fails.

A final true value enters the original block without replacing any of it: the retract sound, Caught/Missed notification, celebration/essence/size blackboard writes, bite-timer reset, and player and bobber Reel transitions remain engine-owned. A final false value stays in Wait. This lets a mod request an automatic catch while the bite timer is active without reproducing the catch pipeline.

With zero handlers the filter returns the captured input result unchanged, so the seam is behaviorally identical to pristine. The dispatch runs every frame while the nested fishing FSM waits for a fish, so handlers should keep their first test cheap.

## See Also

- [fishing.should_reel](../hooks/fishing.should_reel.md) - This is the hook this seam dispatches.
- [fsm_transition](fsm_transition.md) - Filters the later state-transition requests shared by the player, fishing sub-machine, fish, and bobber.
- [input_check_value_id](input_check_value_id.md) - Filters input ids at the general input lookup rather than this fishing-specific decision.
