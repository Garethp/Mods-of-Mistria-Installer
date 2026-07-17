# Seam: input_take_press

Puts a veto between an interaction's pressed read and the interaction running.

`input_take_press` is a **text seam**, a verbatim `anchor`/`replace` edit. It feeds [input.take_press](../hooks/input.take_press.md). Mod authors never write seams. You register handlers for the hooks they dispatch. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/objects/system/parents/par_interactable.gml` |
| **Locator** | text anchor on the `pressed` read in the registered-interaction poll, before the `if pressed || force_press` gate |
| **Feeds** | [`input.take_press`](../hooks/input.take_press.md) |
| **ctx built** | `{ subject: self, input_id: interaction.input_id, local_key: interaction.local_key, interaction: interaction }` |
| **On veto** | `pressed = false;` |
| **Marker** | `mmapi_input_run_take_press_guards` |

## The Edit

The pristine code reads a registered interaction's input (`INPUT.take_press(interaction.input_id)` when the interaction takes the press, else `INPUT.pressed(interaction.input_id)`), then gates the interaction on `if pressed || force_press`. The replace inserts a block between the read and the gate that runs only when the input actually read as pressed: inside `if (pressed)`, it calls `mmapi_check_guards("input.take_press", { subject: self, input_id: interaction.input_id, local_key: interaction.local_key, interaction: interaction })` in a try/catch, and when any guard returns `false` it sets `pressed = false`. On frames where nothing was pressed, no guard is consulted at all.

A veto clears only `pressed`. The `|| force_press` half of the gate is untouched, so a forced press still goes through. The hook vetoes the player's press, not the engine's own forced interactions.

## See Also

- [input.take_press](../hooks/input.take_press.md) - This is the hook this seam dispatches.
- [input_check_value_id](input_check_value_id.md) - This is the other input seam, which remaps the id an input lookup resolves.
