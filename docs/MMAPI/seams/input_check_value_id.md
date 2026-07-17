# Seam: input_check_value_id

Puts a filter on the input id at the head of the engine's input value lookup.

`input_check_value_id` is a **template seam** (`op = "filter"`). It feeds [input.check_value_id](../hooks/input.check_value_id.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/GameplaySystems/Input/Input.gml` |
| **Locator** | pristine context at the head of `check_value(input_id)`, before the `input_overrides` check |
| **Op** | `filter` |
| **Feeds** | [`input.check_value_id`](../hooks/input.check_value_id.md) |
| **Value filtered** | `input_id`, the input id being checked |
| **ctx built** | `undefined` |
| **Marker** | `mmapi_input_run_check_value_id_filters` |

## The Edit

The generated dispatch lands as the first statement of `Input.check_value(input_id)`, before the engine consults `self.input_overrides[input_id]`. It reassigns `input_id = mmapi_apply_filters("input.check_value_id", input_id, undefined)` in the uniform try/catch shape, so a handler's replacement id remaps the whole lookup. The overrides check and everything downstream in `check_value` resolve against the id the filter returned.

With zero handlers the filter hands `input_id` back unchanged and the seam is behaviorally identical to pristine. The dispatch early-outs on an empty registry.

## See Also

- [input.check_value_id](../hooks/input.check_value_id.md) - This is the hook this seam dispatches.
- [input_take_press](input_take_press.md) - This is the other input seam, a veto on interaction presses.
