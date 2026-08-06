using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Tomlyn;

namespace Garethp.ModsOfMistriaInstallerLib.Utils;

/// <summary>
/// Validates TOML supplied by a mod before the archive transaction starts.
/// Keeping this separate from the installers makes the error point to the
/// source mod file rather than to whichever merge operation happened to read
/// it first.
/// </summary>
public static class TomlFileValidator
{
    public static void ValidateMods(IEnumerable<IMod> mods)
    {
        foreach (var mod in mods)
        {
            foreach (var path in mod.GetAllFiles(".toml").Distinct(StringComparer.OrdinalIgnoreCase))
            {
                try
                {
                    Toml.ParseToml(mod.ReadFile(path));
                }
                catch (Exception exception) when (exception is TomlException or FormatException)
                {
                    throw new InvalidDataException(
                        $"Invalid TOML in mod '{mod.GetName()}' ({mod.GetId()}), file '{path}'.",
                        exception);
                }
            }
        }
    }
}
