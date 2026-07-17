# Hook: input.check_value_id

Swap the input id `Input.check_value()` looks up.

`input.check_value_id` is a **filter** hook. Register a callback with `mmapi_filter`. See [Hooks](../HOOKS.md) for how registration and dispatch work.

## Contract

Fires at the top of `Input.check_value()`. The filtered value is the `input_id` being checked. ctx is `undefined`. Return a replacement input id to remap the lookup, or `undefined` to keep the current value.

| | |
| --- | --- |
| **Fires** | At the top of `Input.check_value()`. |
| **Value** | The `input_id` being checked. |
| **ctx** | `undefined` |
| **Kind contract** | The callback receives the current value and returns a replacement, or `undefined` to keep the current value. |

## Usage

```gml
// input.check_value_id is a FILTER: you receive (value, ctx) and return a
// replacement, or undefined to keep the game's value.
function key_swapper_input_check_value_id(_value, _ctx) {
    // _value is the input_id Input.check_value() is about to look up.
    // _ctx is undefined.
    if (_value == undefined) return undefined; // test undefined BEFORE anything else
    // remap one input to another:
    // if (_value == <the input id you remap>) return <the replacement id>;
    return undefined; // undefined = keep the game's value
}

mmapi_filter("input.check_value_id", key_swapper_input_check_value_id);
```

## Engine Wiring

- Seam [`input_check_value_id`](../seams/input_check_value_id.md) dispatches from `gml/scripts/GameplaySystems/Input/Input.gml`, at the head of `check_value()`, filtering `input_id` before the `input_overrides` lookup.

## See Also

- [input.take_press](input.take_press.md) - Block an interactable's press before the interaction runs.
