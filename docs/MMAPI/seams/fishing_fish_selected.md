# Seam: fishing_fish_selected

Emits an event after the fishing hub accepts a fish candidate.

`fishing_fish_selected` is a **text seam** (`context_before` + `context_after`). It feeds [fishing.fish_selected](../hooks/fishing.fish_selected.md). Mod authors register handlers for the hook; they do not write seams. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/Player/FishingHub.gml` |
| **Locator** | the unique `self.bite_timer = 0` assignment inside `__FishHub.set_fish` |
| **Op** | text insertion |
| **Feeds** | [`fishing.fish_selected`](../hooks/fishing.fish_selected.md) |
| **Context** | `{ fishing: self, fish: fish }` |
| **Marker** | `mmapi_fishing_run_fish_selected_callbacks` |

## Behavior

The dispatch is inserted after `interested_fish`, `nibbled_times`, and `bite_timer` have been updated. The original assignments remain engine-owned. The event is guarded so a failing mod callback cannot change the fishing state or block the fish FSM transition.

The seam is shared by normal fish and fish-school selection because both call `FISHING.set_fish(...)`.
