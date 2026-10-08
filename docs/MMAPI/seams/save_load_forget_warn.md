# Engine Fix: save_load_forget_warn

Every save entry dropped for an unresolvable name is named in the log.

`save_load_forget_warn` is an **engine fix**, an anchored edit with no hook behind it. Nothing dispatches. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/Utilities/ArrayBool.gml` |
| **Locator** | text anchor on `deserialize_array_bool`'s skip arm |
| **Feeds** | (no hook) |
| **Marker** | `mmapi_save_forget_warn` |

## The Edit

The replace adds an else arm to the skip. After the `is_numeric(num)` branch that writes the entry and the `DEBUG_ASSERTIONS` branch that crashes, a final else logs a warn naming the dropped entry, with the marker as a trailing comment.

`deserialize_array_bool` is the shared restore path for every by-name boolean roster in the save, which covers perks, items, recipes, tutorials, and, with [save_load_spells_tolerance](save_load_spells_tolerance.md), spells. The shipped game already drops unresolvable entries silently, while its debug-only `crash` arm treats an unknown name as an event worth reporting. The fix adds that report to the shipped game as a warn, one per dropped name, so a player or mod author reading the log after an uninstall sees exactly what the save lost instead of inferring it. The `DEBUG_ASSERTIONS` crash arm is preserved untouched.

On an intact install every name resolves and the new arm never runs.

## See Also

- [save_load_spells_tolerance](save_load_spells_tolerance.md) - Routes unknown spells into the arm this fix reports.
- [save_load_used_objects_tolerance](save_load_used_objects_tolerance.md) - Routes unknown object names into the same arm.
- [Debug](../DEBUG.md#when-a-save-silently-refuses-to-load) - Reading these warns when a save refuses to load.
