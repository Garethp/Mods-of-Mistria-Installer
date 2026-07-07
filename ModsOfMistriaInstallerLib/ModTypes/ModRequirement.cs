using System.Text.RegularExpressions;

namespace Garethp.ModsOfMistriaInstallerLib.ModTypes;

public record ModRequirement(string Name, string Author, string? DownloadUrl = null)
{
    public string GetId()
    {
        var raw = $"{Author.ToLower()}.{Name.ToLower()}".Replace(" ", "_");
        return Regex.Replace(raw, "[^a-zA-Z0-9_\\.]", "");
    }
}
