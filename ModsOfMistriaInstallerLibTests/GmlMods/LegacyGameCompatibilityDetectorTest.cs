using Garethp.ModsOfMistriaInstallerLib.GmlMods;
using ModsOfMistriaInstallerLibTests.Fixtures;

namespace ModsOfMistriaInstallerLibTests.GmlMods;

public class LegacyGameCompatibilityDetectorTest
{
    [Test]
    public void DetectsKnownPre103GmlSignature()
    {
        var mod = new MockMod(new Dictionary<string, object>
        {
            ["gml/old.gml"] = "if self.parent_object() != obj_monster_rock_stack { continue; }"
        });

        var findings = LegacyGameCompatibilityDetector.Find(mod);

        Assert.That(findings, Has.Some.Contains("legacy 1.0.2 GML"));
    }

    [Test]
    public void DetectsLegacyLoadingScreenPair()
    {
        var mod = new MockMod(new Dictionary<string, object>
        {
            ["animations/UI NEW/Loading Screen/spr_loading_bird_en.png"] = Array.Empty<byte>(),
            ["animations/UI NEW/Loading Screen/spr_loading_bird_id.png"] = Array.Empty<byte>()
        });

        var findings = LegacyGameCompatibilityDetector.Find(mod);

        Assert.That(findings, Has.Some.Contains("loading-screen replacement"));
    }

    [Test]
    public void ExcludesBulgarianLocalization()
    {
        var mod = new MockMod(new Dictionary<string, object>
        {
            ["gml/old.gml"] = "DUNGEON_RUNNER = new DungeonRunner(itinerary, start_floor);"
        }) { Id = "actepukc.bulgarian_localization" };

        Assert.That(LegacyGameCompatibilityDetector.Find(mod), Is.Empty);
    }
}
