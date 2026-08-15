# Engine Fix: save_load_forget_warn

Every save entry dropped for an unresolvable name is named in the log.

`save_load_forget_warn` is an **engine fix**, a hook-less edit. It dispatches nothing, and there is no handler to register. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/Utilities/ArrayBool.gml` |
| **Locator** | text anchor on `deserialize_array_bool`'s skip arm |
| **Op** | text (new else arm) |
| **Marker** | `mmapi_save_forget_warn` |

## The Edit

```gml
        var num = string_to_num(string_array[i]);
        if is_numeric(num) {
            target[num] = true;
        } else if DEBUG_ASSERTIONS {
            crash("unexpected input in functor...{}/{} -> {}", i, string_array[i], num);
        } else {
            warn("MMAPI: save carried unknown entry '{}' - dropped", string_array[i]); // mmapi_save_forget_warn
        }
```

## Why

`deserialize_array_bool` is the shared restore path for every by-name boolean roster in the save: perks, items, recipes, tutorials, and, with [save_load_spells_tolerance](save_load_spells_tolerance.md), spells. Vanilla already drops unresolvable entries silently in retail builds, while the debug-build `crash` arm shows the developers considered an unknown name reportable. This fix gives retail the report, one warn per dropped name, so a player or mod author reading the log after an uninstall sees exactly what the save lost instead of inferring it. The `DEBUG_ASSERTIONS` crash arm is preserved untouched.

Zero-registrant inert: on an intact install every name resolves and the new arm never runs.
