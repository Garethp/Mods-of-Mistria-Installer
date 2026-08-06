using System.Reflection;

namespace Garethp.ModsOfMistriaGUI;

public static class AppInfo
{
    public const string GitHubUrl = "https://github.com/AcTePuKc/Mods-of-Mistria-Installer";
    public const string ReleaseApiUrl = "https://api.github.com/repos/Garethp/Mods-of-Mistria-Installer/releases/latest";
    public const string SupportedGame = "Fields of Mistria 1.0.x";

    public static string Version
    {
        get
        {
            var assembly = Assembly.GetEntryAssembly();
            // InformationalVersion may be supplied by CI/source-control tooling
            // (for example, a game/mod version). The project FileVersion is the
            // authoritative MOMI application version shown to users.
            var value = assembly?.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version
                        ?? assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                        ?? assembly?.GetName().Version?.ToString();
            return value is null ? "unknown" : TrimBuildSuffix(value);
        }
    }

    public static string DisplayVersion => $"MOMI {Version}";

    private static string TrimBuildSuffix(string value)
    {
        var plus = value.IndexOf('+');
        if (plus >= 0) value = value[..plus];
        if (System.Version.TryParse(value, out var version) && version.Revision == 0)
            return version.ToString(3);
        return value;
    }
}
