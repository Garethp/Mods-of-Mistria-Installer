# MMAPI

MMAPI is a GML modding framework for Fields of Mistria. It provides mod
identity and lifecycle, a named-hook registry, hotkeys, localisation, per-save
mod data, and an in-game debugging agent. Mods written in GML call into it
rather than editing the game directly.

MMAPI was written by Anna Nomoly. It is distributed as part of the Mods of
Mistria Installer, which installs these sources into the game alongside the
seam catalog that wires them to the engine.

## Licensing

MMAPI is licensed under the GNU General Public License version 3 or later,
with additional terms under GPLv3 section 7. Those additional terms cover
attribution preservation, misrepresentation of origin, and trademark, and
they are stated in full in the `LICENSE` file in this directory. Every source
file here carries a header pointing to it.

The full GPL text is in `LICENCE.txt` at the root of this repository.

The seam catalog is not in this directory. It contains excerpts of Fields of
Mistria game code owned by NPC Studio, and it carries its own notice at the
top of the file.
