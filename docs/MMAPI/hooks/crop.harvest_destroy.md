# Hook: crop.harvest_destroy

Change whether a harvested crop node is destroyed.

`crop.harvest_destroy` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for registration and dispatch details.

## Contract

The hook fires in `process_crop_harvest()` after the engine has applied its managed, regrowing, and forageable rules, but immediately before the node is destroyed or retained.

The incoming value is the engine's `destroy` boolean. `ctx` contains:

- `ctx.node` — the crop node;
- `ctx.harvester_cardinal` — the direction used for the harvest sway.

Return `true` to destroy the node, `false` to keep the node and use the normal retained/regrowth path, or `undefined` to keep the engine decision.

This hook does not change the item drop, farming XP, or harvest statistics. Those are handled by the caller before `process_crop_harvest()`.

## Usage

```gml
function keep_special_crop(_destroy, _ctx) {
    if (_ctx.node.prototype.object_id == ObjectId.SpecialCrop) return false;
    return undefined;
}

mmapi_filter("crop.harvest_destroy", keep_special_crop);
```

With no handlers, the engine's original destruction/regrowth decision is unchanged.
