using Garethp.ModsOfMistriaInstallerLib.ModTypes;

namespace Garethp.ModsOfMistriaInstallerLib.GmlMods;

/// <summary>
/// Finds narrow signatures of pre-1.0.3 game patches. This is deliberately
/// limited to signatures known to target engine/asset changes from 1.0.2;
/// generic legacy GML is not enough to reject a mod.
/// </summary>
public static class LegacyGameCompatibilityDetector
{
    private const string BulgarianLocalizationId = "actepukc.bulgarian_localization";

    public static IReadOnlyList<string> Find(IMod mod)
    {
        // The Bulgarian package carries compatibility assets and a hook of its
        // own. It is known-good for our current 1.0.3 test path and must not
        // become a false positive in its own installer.
        if (mod.GetId().Equals(BulgarianLocalizationId, StringComparison.OrdinalIgnoreCase))
            return [];

        var findings = new List<string>();
        var gmlFiles = mod.GetAllFiles(".gml");
        foreach (var file in gmlFiles)
        {
            var text = mod.ReadFile(file);
            if (ContainsAny(text,
                    "DUNGEON_RUNNER = new DungeonRunner",
                    "goto_gm_room(DUNGEON_RUNNER.current_level()",
                    "self.parent_object() != obj_monster_rock_stack",
                    "mmapi_statue_hp_sweep"))
            {
                findings.Add("legacy 1.0.2 GML patch signature (dungeon/rock-stack/statue compatibility)");
                break;
            }
        }

        // Before 1.0.3, several language mods replaced the English loading
        // screen and added a second language-named sprite. The 1.0.3 asset
        // layout changed, so this pair is a useful signature for old packages.
        var files = mod.GetAllFiles("")
            .Select(Normalize)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        const string loadingPrefix = "animations/ui new/loading screen/spr_loading_bird_";
        var englishLoading = files.Any(path =>
            (path.Equals("animations/ui new/loading screen/spr_loading_bird_en.png",
                StringComparison.OrdinalIgnoreCase)
             || path.EndsWith("/animations/ui new/loading screen/spr_loading_bird_en.png",
                StringComparison.OrdinalIgnoreCase)));
        var localizedLoading = files.Any(path =>
            (path.Contains("/" + loadingPrefix, StringComparison.OrdinalIgnoreCase)
             || path.StartsWith(loadingPrefix, StringComparison.OrdinalIgnoreCase))
            && path.Contains("spr_loading_bird_", StringComparison.OrdinalIgnoreCase)
            && path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
            && !path.EndsWith("_en.png", StringComparison.OrdinalIgnoreCase));
        if (englishLoading && localizedLoading)
            findings.Add("legacy pre-1.0.3 loading-screen replacement");

        return findings;
    }

    private static bool ContainsAny(string text, params string[] signatures) =>
        signatures.Any(signature => text.Contains(signature, StringComparison.OrdinalIgnoreCase));

    private static string Normalize(string path) => path.Replace('\\', '/').TrimStart('/');
}
