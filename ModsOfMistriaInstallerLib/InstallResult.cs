using Garethp.ModsOfMistriaInstallerLib.Lang;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;

namespace Garethp.ModsOfMistriaInstallerLib;

// One skipped mod and why. Reasons also land as Validation errors on the mod,
// so the per-mod expander shows them; this record feeds the status line and
// the install-state manifest.
public record SkippedMod(string Id, string Version, IReadOnlyList<string> Reasons);

// Per-mod outcomes of one install run, returned by InstallMods (D12). A mod
// is installed whole or skipped whole; a skipped mod's content is excluded
// with its behaviour.
public class InstallResult
{
    public List<IMod> Installed { get; } = [];

    public List<SkippedMod> Skipped { get; } = [];

    // The status-line summary, e.g. "3 mod(s) installed, 1 skipped". The
    // per-mod icons and expanders name which mods were skipped and why.
    public string Summary() => Skipped.Count > 0
        ? string.Format(Resources.CoreInstallSummaryWithSkipped, Installed.Count, Skipped.Count)
        : string.Format(Resources.CoreInstallSummary, Installed.Count);
}
