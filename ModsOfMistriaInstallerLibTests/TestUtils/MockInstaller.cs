using Garethp.ModsOfMistriaInstallerLib;
using Garethp.ModsOfMistriaInstallerLib.Collector;
using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.Installer;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Garethp.ModsOfMistriaInstallerLib.Utils;

namespace ModsOfMistriaInstallerLibTests.TestUtils;

public class MockInstaller
{
    public void InstallMod(IMod mod, IFileModifier fileModifier)
    {
        var fileNameUIDMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var generatedInformation = new GeneratedInformation();
        
        // 0. Expand momi/ compact definitions into virtual overlay files
        var generated = new OutfitGenerator().Generate(mod);
        foreach (var kvp in new FurnitureGenerator().Generate(mod))
            generated.TryAdd(kvp.Key, kvp.Value);

        var redirects = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        new CompactFurnitureGenerator().Generate(mod, generated, redirects);

        IMod effectiveMod = generated.Count > 0 || redirects.Count > 0
            ? new GeneratedOverlayMod(mod, generated, redirects)
            : mod;
  
        generatedInformation.Merge(new TOMLCollector().Collect(effectiveMod));
        
        // 2. Install TOML files (uses IDs populated above)
        new TOMLInstaller(fileNameUIDMapping, fileModifier)
            .Install(effectiveMod, generatedInformation, (_, _) => { });

        // 3. Install JSON files
        new JSONInstaller(fileNameUIDMapping, fileModifier)
            .Install(effectiveMod, generatedInformation, (_, _) => { });

        // 4. Install XML files
        new XMLInstaller(fileNameUIDMapping, fileModifier)
            .Install(effectiveMod, generatedInformation, (_, _) => { });

        // 5. Install MIST files (overwrite)
        new MISTInstaller(fileNameUIDMapping, fileModifier)
            .Install(effectiveMod, generatedInformation, (_, _) => { });

        // 6. Generate data-layer content from momi/ definitions (fiddle, outlines, asset_parts)
        new OutfitInstaller(fileNameUIDMapping, fileModifier)
            .Install(mod, generatedInformation, (_, _) => { });
        
        new FurnitureInstaller(fileNameUIDMapping, fileModifier)
            .Install(mod, generatedInformation, (_, _) => { });
    }
}