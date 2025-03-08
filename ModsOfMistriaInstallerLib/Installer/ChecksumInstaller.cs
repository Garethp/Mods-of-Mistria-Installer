using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib.Installer;

public class ChecksumInstaller
{
    private readonly List<string> _customKeys =
    [
        "mods_installed",
        "mods",
        "graphics_mods",
        "dll_mods",
        "fiddle_mods",
        "t2_mods",
        "cutscene_mods",
        "other_mods"
    ];
    
    public void Install(
        string fieldsOfMistriaLocation,
        string modsLocation,
        List<GeneratedInformationWithMod> mods,
        Action<string, string> reportStatus
        ) {
        var checksums = JObject.Parse(File.ReadAllText(Path.Combine(fieldsOfMistriaLocation, "checksums.json")));
        
        foreach (var key in _customKeys)
        {
            checksums.Remove(key);
        }

        checksums["mist"] = new FileInfo(Path.Combine(fieldsOfMistriaLocation, "__mist__.json")).Length;
        checksums["fiddle"] = new FileInfo(Path.Combine(fieldsOfMistriaLocation, "__fiddle__.json")).Length;
        checksums["t2_input"] = new FileInfo(Path.Combine(fieldsOfMistriaLocation, "t2_input.json")).Length;
        checksums["t2_output"] = new FileInfo(Path.Combine(fieldsOfMistriaLocation, "t2_output.json")).Length;
        checksums["localization"] = new FileInfo(Path.Combine(fieldsOfMistriaLocation, "localization.json")).Length;

        if (mods.Count > 0)
        {
            var modsInstalled = new JArray();

            foreach (var mod in mods)
            {
                var modInformation = new JObject();
                modInformation["id"] = mod.Mod.GetId();
                modInformation["name"] = mod.Mod.GetName();
                modInformation["author"] = mod.Mod.GetAuthor();
                modInformation["version"] = mod.Mod.GetVersion();

                modInformation["graphics"] = mod.Sprites.Count > 0 || mod.Tilesets.Count > 0;
                modInformation["dll"] = mod.AurieMods.Count > 0;
                modInformation["fiddle"] =
                    mod.Fiddles.Count > 0 || mod.StoreCategories.Count > 0 || mod.StoreItems.Count > 0;
                modInformation["t2"] = mod.Localisations.Count > 0 || mod.Conversations.Count > 0 ||
                                       mod.Schedules.Count > 0;
                modInformation["cutscenes"] = mod.Cutscenes.Count > 0;
                modInformation["other"] = mod.Points.Count > 0 || mod.Outlines.Count > 0 || mod.AssetParts.Count > 0;

                modsInstalled.Add(modInformation);
            }

            checksums["mods_installed"] = true;

            checksums["graphics_mods"] = modsInstalled.Any(mod => mod["graphics"]?.Value<bool>() == true);
            checksums["dll_mods"] = modsInstalled.Any(mod => mod["dll"]?.Value<bool>() == true);
            checksums["fiddle_mods"] = modsInstalled.Any(mod => mod["fiddle"]?.Value<bool>() == true);
            checksums["t2_mods"] = modsInstalled.Any(mod => mod["t2"]?.Value<bool>() == true);
            checksums["cutscene_mods"] = modsInstalled.Any(mod => mod["cutscene"]?.Value<bool>() == true);
            checksums["other_mods"] = modsInstalled.Any(mod => mod["other"]?.Value<bool>() == true);

            checksums["mods"] = modsInstalled;
        }

        File.WriteAllText(
            Path.Combine(fieldsOfMistriaLocation, "checksums.json"),
            checksums.ToString()
        );
    }

    public void Uninstall(string fieldsOfMistriaLocation)
    {
        var checksums = JObject.Parse(File.ReadAllText(Path.Combine(fieldsOfMistriaLocation, "checksums.json")));



        checksums["mist"] = new FileInfo(Path.Combine(fieldsOfMistriaLocation, "__mist__.json")).Length;
        checksums["fiddle"] = new FileInfo(Path.Combine(fieldsOfMistriaLocation, "__fiddle__.json")).Length;
        checksums["t2_input"] = new FileInfo(Path.Combine(fieldsOfMistriaLocation, "t2_input.json")).Length;
        checksums["t2_output"] = new FileInfo(Path.Combine(fieldsOfMistriaLocation, "t2_output.json")).Length;
        checksums["localization"] = new FileInfo(Path.Combine(fieldsOfMistriaLocation, "localization.json")).Length;

        foreach (var key in _customKeys)
        {
            checksums.Remove(key);
        }

        File.WriteAllText(
            Path.Combine(fieldsOfMistriaLocation, "checksums.json"),
            checksums.ToString()
        );

    }
}