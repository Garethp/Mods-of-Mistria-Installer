# Engine Fix: eod_intro_fade_cancel

Keeps the end-of-day menu's intro fade-in chain in a field and clears the field when it fires, so a Next Day tap that lands before the fade-in starts can cancel it.

`eod_intro_fade_cancel` is an **engine fix**, an anchored edit with no hook behind it. Nothing dispatches. Together with [eod_prep_cancels_intro_fade](eod_prep_cancels_intro_fade.md) it closes a race in the vanilla end-of-day sequence that leaves the Next Day button locked and dead. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/UI/Anchor/Menus/EodMenu.gml` |
| **Locator** | text anchor: the three-line chain in the fade-out callback of `init()` that waits one tick and then calls `SCREEN_FADER.fade_in` |
| **Feeds** | (no hook) |
| **Marker** | `mmapi_eod_intro_fade_cancel` |

## The Edit

`init()` calls `SCREEN_FADER.fade_out(240, callback)`. The callback spawns the receipt and the Summary and Next Day buttons, then queues a chain built with `append` as `[Wait, Timer(1), Wait, Function]`, whose Function link is a bare reference to `SCREEN_FADER.fade_in` with `FADE_SPEED_CUTSCENE` as its argument. The chain fires the fade-in on its second pass through the chain manager. The buttons take input from the frame they exist, while the screen is still black, because nothing locks them until the fade-in has started.

A Next Day tap runs `prep_for_sequence()`, which locks both buttons, cancels the farm tour chain, calls `SCREEN_FADER.fade_out(FADE_SPEED_TRANSITION)`, stops the music, and awaits `SCREEN_FADER.is_out()`. A tap that lands after the intro chain's first pass and before its second finds the fader already at alpha 1, so that fade-out is a zero-length no-op. The intro chain's second pass then starts the 120-frame fade-in before the tap's own chain evaluates its await. The fader runs to alpha 0 and `is_out()` stays false for the rest of the session. The receipt stays on screen with both buttons locked, the farm view never pans, the music stays silent, and the day is never saved. A press of Interact during the black transition lands in that window.

The window's frame depends on how the day ended. After sleeping, the screen is fully black when the menu spawns, so the 240-frame fade-out is zero-length and the callback runs synchronously inside `init()`, in the chain pass that spawned the menu. The intro chain gets its first pass in that same chain pass, and the window is the anchor pass of that frame. After a pass-out, the fade-out runs its 240 frames, the callback runs from a fader think, and the window is one frame later.

The replace assigns the chain to `self.intro_chain` and turns the bare function reference into a closure that clears the field before it calls `SCREEN_FADER.fade_in(FADE_SPEED_CUTSCENE)`. The fade-in itself, its duration, and the tick it fires on are unchanged, and the marker sits as a trailing comment on the closing line. With the companion edit staged, a tap in the window cancels the stored chain before its second pass, the fader stays at alpha 1, `is_out()` is true on the next chain pass, and the calendar sequence runs as it does for a tap on the visible receipt. A tap after the fade-in has started follows the vanilla path, because the field is already cleared. With no tap in the window the chain fires on its second pass as before, so the sequence behaves as vanilla.

## See Also

- [eod_prep_cancels_intro_fade](eod_prep_cancels_intro_fade.md) - This is the companion edit that cancels the stored chain in `prep_for_sequence()`.
- [ui_eod_calendar_events](ui_eod_calendar_events.md) - Filters the calendar's events later in the same sequence.
- [game_step_begin_installs](game_step_begin_installs.md) - Another of the catalog's engine fixes, the MMAPI lifecycle root.
