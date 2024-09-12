using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstaller.Installer;

public class ChecksumInstaller: IModuleInstaller
{
    public void Install(string fieldsOfMistriaLocation, GeneratedInformation information)
    {
        var checksums = JObject.Parse(File.ReadAllText(Path.Combine(fieldsOfMistriaLocation, "checksums.json")));

        checksums["mist"] = new FileInfo(Path.Combine(fieldsOfMistriaLocation, "__mist__.json")).Length;
        checksums["fiddle"] = new FileInfo(Path.Combine(fieldsOfMistriaLocation, "__fiddle__.json")).Length;
        checksums["t2_input"] = new FileInfo(Path.Combine(fieldsOfMistriaLocation, "t2_input.json")).Length;
        checksums["t2_output"] = new FileInfo(Path.Combine(fieldsOfMistriaLocation, "t2_output.json")).Length;
        checksums["localization"] = new FileInfo(Path.Combine(fieldsOfMistriaLocation, "localization.json")).Length;
        checksums["mods_installed"] = true;

        File.WriteAllText(
            Path.Combine(fieldsOfMistriaLocation, "checksums.json"),
            checksums.ToString()
        );
    }
}