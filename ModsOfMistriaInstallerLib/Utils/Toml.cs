using Tomlyn;
using Tomlyn.Model;

namespace Garethp.ModsOfMistriaInstallerLib.Utils;

public static class Toml
{
    public static TomlTable LoadToml(string path) =>
        TomlSerializer.Deserialize<TomlTable>(File.ReadAllText(path));

    public static void SaveToml(TomlTable data, string path) =>
        File.WriteAllText(path, TomlSerializer.Serialize(data));
}
