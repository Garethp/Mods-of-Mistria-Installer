namespace Garethp.ModsOfMistriaInstallerLib.GmlMods;

// One lint finding. File is the mod-relative gml path, empty for cross-mod
// findings (whose Line is 0); the file-bearing/file-less split is D12's
// escalation boundary.
public record LintFinding(string ModId, string File, int Line, string Message)
{
    public override string ToString()
    {
        var where = File.Length > 0 ? $"{File}:{Line}: " : "";
        return $"mod '{ModId}': {where}{Message}";
    }
}
