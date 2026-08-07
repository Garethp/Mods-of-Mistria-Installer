# Hook: fishing.fish_selected

Observe when the fishing system accepts a fish candidate.

`fishing.fish_selected` is an **event** hook. Register a callback with `mmapi_on`. See [Hooks](../HOOKS.md) for registration and dispatch details.

## Contract

The event fires after the fishing hub stores the selected fish and resets its nibble and bite state. It is emitted for both ordinary fish and fish-school candidates.

The callback receives `ctx` with:

- `ctx.fishing` — the global fishing hub;
- `ctx.fish` — the selected fish value.

This is observation-only. The event does not replace the selected fish. Use `fishing.should_reel` for changing the reel decision, and `fsm.transition` for controlled state-machine transitions.

## Usage

```gml
function log_selected_fish(_ctx) {
    // Inspect _ctx.fish or record a mod-specific statistic.
}

mmapi_on("fishing.fish_selected", log_selected_fish);
```

With no handlers, the seam only emits a guarded callback after the original state assignments, so the vanilla fishing state is unchanged.
