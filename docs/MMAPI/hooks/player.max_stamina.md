# Hook: player.max_stamina

Change the player's maximum stamina.

`player.max_stamina` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for registration and dispatch details.

## Contract

The hook fires when `Ari.get_max_stamina()` computes the stamina ceiling. The incoming value already includes the engine's base stamina and the Tireless equipment bonus.

The callback receives `ctx.player`. Return a non-negative numeric replacement, or `undefined` to keep the current value. Non-numeric results are ignored and negative numeric results are clamped to zero.

Because the function is used by stamina recovery, clamping, spells, status effects, new-day recovery, and the Vitals UI, this hook changes the shared stamina ceiling rather than one individual stamina gain.

## Usage

```gml
function expand_max_stamina(_value, _ctx) {
    return _value + 25;
}

mmapi_filter("player.max_stamina", expand_max_stamina);
```

Return `undefined` when the handler has no opinion. With no handlers, the original base-plus-equipment value is returned unchanged.
