using System.Text;
using Garethp.ModsOfMistriaInstallerLib.GmlMods;
using Garethp.ModsOfMistriaInstallerLib.Seam;
using Garethp.ModsOfMistriaInstallerLib.Tools;
using ModsOfMistriaInstallerLibTests.Fixtures;

namespace ModsOfMistriaInstallerLibTests.TestUtils;

// The synthetic install fixture the layer tests stage against: a two-file
// pristine engine and a catalog whose anchors match it, the same trick the
// source's install suite used. No real engine needed.
public static class SyntheticLayer
{
    public const string PristineGame = "function step_begin() {\n}\n";

    public const string PristineOther = "function helper() {\n    return 1;\n}\n";

    public const string CatalogToml = """
        version = 2

        [[hook]]
        name = "game.step_begin"
        kind = "event"
        doc  = "Fires at the top of Game.step_begin(). ctx is undefined."

        [[hook]]
        name = "game.legacy"
        kind = "event"
        doc  = "A renamed hook kept as an alias target."
        provider = "runtime"
        aliases = ["game.old_name"]

        [[seam]]
        id = "game_step"
        file = "gml/objects/Game.gml"
        anchor = '''
        function step_begin() {
        }'''
        replace = '''
        function step_begin() {
            mmapi_run_installs(); // __momi_test_game_step
            try { mmapi_emit("game.step_begin", undefined); } catch (__momi_test_step) {}
        }'''
        marker = "__momi_test_game_step"
        provides = ["game.step_begin"]

        [[engine_fix]]
        name = "other_fix"
        file = "gml/objects/Other.gml"
        anchor = '''
        function helper() {
            return 1;
        }'''
        replace = '''
        function helper() {
            return 1; // __momi_test_other_fix
        }'''
        marker = "__momi_test_other_fix"
        """;

    public static SeamCatalog Catalog() =>
        SeamCatalogLoader.Load(Encoding.UTF8.GetBytes(CatalogToml), "synthetic");

    public static MemoryPristineSource Pristine(string game = PristineGame, string other = PristineOther) =>
        new(new Dictionary<string, byte[]>
        {
            { "assets/gml/objects/Game.gml", Encoding.UTF8.GetBytes(game) },
            { "assets/gml/objects/Other.gml", Encoding.UTF8.GetBytes(other) },
            { "assets/fiddle/monsters/shroom.toml", Encoding.UTF8.GetBytes("""
                [default]
                hp = 10
                damage = 2
                essence = 0
                iframes = 7
                damage_number_offset = -15
                patience_acknowledgement_reset = -120
                aggro_radius = 192
                status_effect_offset = -8
                fire_effect_offset = -4
                starting_dir = [0, 360]
                gm_object = "obj_monster_mushroom"
                hurtbox = "spr_native"
                drops = []
                [default.tango]
                [mushroom]
                [mushroom.sprites]
                idle = "spr_native"
                """) },
        });

    public static GmlModCode Mod(string id, string gml = "// state\n", string? dirName = null,
        List<string>? requiresHooks = null)
    {
        var mock = new MockMod(new Dictionary<string, object> { { "gml/core/State.gml", gml } })
        {
            Id = id,
            DirName = dirName ?? id,
            Version = "0.0.1",
            RequiredHooks = requiresHooks ?? [],
        };
        return GmlModCollector.Collect(mock)!;
    }
}

// A scripted compile gate: records every call, fails when told to, exactly
// where the source's tests patched run_compile_pass
public class ScriptedGate : ICompileGate
{
    public List<(string Mode, List<string> Paths)> Calls { get; } = [];

    // (mode, paths) → failure message, or null to pass
    public Func<string, IReadOnlyList<string>, string?>? Fails { get; init; }

    public bool Available => true;

    public void RunFiles(IReadOnlyList<string> paths) => Run("files", paths);

    public void RunUnit(IReadOnlyList<string> paths) => Run("unit", paths);

    private void Run(string mode, IReadOnlyList<string> paths)
    {
        Calls.Add((mode, paths.ToList()));
        var failure = Fails?.Invoke(mode, paths);
        if (failure is not null)
            throw new InvalidOperationException($"compile pass FAILED (exit 1):\n{failure}");
    }
}
