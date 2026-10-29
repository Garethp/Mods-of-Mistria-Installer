# Engine Fix: title_momi_version

Labels the title screen with the MOMI release that built the archive, one row above the version line the engine already draws there.

`title_momi_version` is an **engine fix**, an anchored edit with no hook behind it. Nothing dispatches. It makes a modified title screen say which installer release produced the archive, where the engine's own label only says that something did. See [Seams](../SEAMS.md).

## Placement

| | |
| --- | --- |
| **File** | `gml/scripts/UI/Anchor/Menus/TitleMenu.gml` |
| **Locator** | text anchor on the close of the version label's think callback in `setup_main_screen` |
| **Feeds** | (no hook) |
| **Marker** | `mmapi_title_momi_version` |

## The Edit

The title menu's `setup_main_screen` builds the bottom-left version label as a text node whose think callback rebuilds the string every frame. The string is the game version from `GAME_VERSION`, suffixed with `(modified)` whenever the runtime reports the archive or the command line as altered.

The replace re-emits the close of that callback and follows it with one more text node on the menu canvas, in the same font and color table and aligned to the same corner, reading `MOMI v` followed by the release. The label takes the first free row above the labels the engine's debug builds stack there and never overlaps them. The release is read from the install stamp through the accessor form, and when the stamp is absent the label is skipped and the screen is unmodified. The closing brace carries the `mmapi_title_momi_version` marker as a trailing comment.

MOMI writes the stamp, `mmapi_install_stamp.gml`, beside the generated hook catalog on every install that stages the GML layer. It holds one global, `__mmapi_installer_version`, set to the running installer's file version. The label therefore appears whenever a MOMI build is live, and it names the release that produced it.

## See Also

- [setup_title_entry](setup_title_entry.md) - The seam that announces each title entry from Setup's create, frames before this label is built.
- [game_step_begin_installs](game_step_begin_installs.md) - Another of the catalog's engine fixes, the MMAPI lifecycle root.
