using Tomlyn;
using Tomlyn.Model;

namespace Garethp.ModsOfMistriaInstallerLib.Utils;

public static class Toml
{
    public static TomlTable ParseToml(string content) =>
        TomlSerializer.Deserialize<TomlTable>(content);
}
