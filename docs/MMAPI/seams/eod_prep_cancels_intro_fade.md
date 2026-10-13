# Engine Fix: eod_prep_cancels_intro_fade

Cancels the stored intro fade-in chain when the player commits to the end of the day, so a tap during the black transition no longer strands the sequence waiting on `is_out()`.

`eod_prep_cancels_intro_fade` is an **engine fix**, an anchored edit with no hook behind it. Nothing dispatches. It is the companion of [eod_intro_fade_cancel](eod_intro_fade_cancel.md), which stores the chain this edit cancels and describes the race the pair closes. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/UI/Anchor/Menus/EodMenu.gml` |
| **Locator** | text anchor: the four-line block in `prep_for_sequence()` that cancels `life_chain`, after the `has_prepped` early return |
| **Feeds** | (no hook) |
| **Marker** | `mmapi_eod_prep_cancels_intro_fade` |

## The Edit

`prep_for_sequence()` runs once per end of day, from the Next Day tap. It returns an empty chain when `has_prepped` is already set, cancels `life_chain` when one is running, calls `SCREEN_FADER.fade_out(FADE_SPEED_TRANSITION)`, sets `has_prepped`, stops the music, and returns the chain that awaits `SCREEN_FADER.is_out()`. The pristine function has no knowledge of the intro chain its own `init()` queued, so a tap during the black transition leaves that chain free to fade the screen in after the await has been built.

The replace keeps the `life_chain` block and appends a second block of the same shape for `intro_chain`, before the fade-out call. It reads the field through the struct accessor `self[$ "intro_chain"]`, so it is a no-op when the chain has already fired and cleared the field, and also when the field was never set. `CHAINS.cancel_chain` only clears the chain's running flag, and the chain manager drops the chain on its next pass without running the fade-in. The marker sits as a trailing comment on the new `if` line.

Ordering between the two edits is not required for the text to apply. This block anchors on pristine text the first edit never touches, and the accessor read keeps it harmless on its own. With no tap during the black transition the intro chain has already fired by the time the player taps, the field is undefined, and the sequence behaves as vanilla.

## See Also

- [eod_intro_fade_cancel](eod_intro_fade_cancel.md) - This is the companion edit that stores the chain, with the account of the race.
- [ui_eod_notification_custom_entry](ui_eod_notification_custom_entry.md) - The other edit in the same file, the custom row case in `build_notifications()`.
- [max_crafts_zero_component](max_crafts_zero_component.md) - Another engine fix that corrects a pristine control-flow gap.
